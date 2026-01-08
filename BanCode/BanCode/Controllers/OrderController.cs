using BanCode.Helpers;
using BanCode.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BanCode.Services.VnPay; // Đảm bảo đã có namespace này

namespace BanCode.Controllers
{
    [Authorize] // Bắt buộc đăng nhập mới xem được
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IVnPayService _vnPayService; // Khai báo service

        // --- 1. SỬA LỖI CONSTRUCTOR: Thêm tham số vnPayService ---
        public OrderController(ApplicationDbContext context, IWebHostEnvironment env, IVnPayService vnPayService)
        {
            _context = context;
            _env = env;
            _vnPayService = vnPayService; // Gán giá trị
        }

        // 1. TRANG THANH TOÁN (GET)
        public IActionResult Checkout()
        {
            var cart = HttpContext.Session.Get<List<CartItem>>("Cart") ?? new List<CartItem>();

            if (cart.Count == 0)
            {
                return RedirectToAction("Index", "Cart");
            }

            ViewBag.Cart = cart;
            ViewBag.TotalAmount = cart.Sum(item => item.Price);

            return View();
        }

        // 2. XỬ LÝ THANH TOÁN (POST)
        [HttpPost]
        // --- SỬA LỖI LOGIC: Thêm tham số paymentMethod ---
        public async Task<IActionResult> ProcessPayment(string fullName, string email, string paymentMethod)
        {
            var cart = HttpContext.Session.Get<List<CartItem>>("Cart") ?? new List<CartItem>();
            if (cart.Count == 0) return RedirectToAction("Index", "Cart");

            // 1. Tạo User nếu chưa có
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                user = new User
                {
                    Id = Guid.NewGuid(),
                    FullName = fullName,
                    Email = email,
                    CreatedAt = DateTime.Now
                };
                _context.Users.Add(user);
            }

            // 2. Tạo Order
            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TotalAmount = cart.Sum(i => i.Price),
                Status = "pending", // Mặc định là chờ
                CreatedAt = DateTime.Now
            };
            _context.Orders.Add(order);

            // 3. Tạo OrderItem
            foreach (var item in cart)
            {
                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    PackageId = item.PackageId,
                    PriceAtPurchase = item.Price
                };
                _context.OrderItems.Add(orderItem);
            }

            // Lưu đơn hàng vào DB trước (Trạng thái Pending)
            await _context.SaveChangesAsync();

            // Xóa giỏ hàng
            HttpContext.Session.Remove("Cart");

            // --- SỬA LỖI LOGIC: Chuyển hướng sang VNPay nếu được chọn ---
            if (paymentMethod == "VnPay")
            {
                // Tạo URL thanh toán
                var paymentUrl = _vnPayService.CreatePaymentUrl(HttpContext, order);
                // Chuyển hướng người dùng sang VNPay
                return Redirect(paymentUrl);
            }
            // -------------------------------------------------------------

            // Nếu thanh toán thường (QR/COD) -> Chuyển sang trang Payment nội bộ
            return RedirectToAction("Payment", new { orderId = order.Id });
        }

        // 3. NHẬN KẾT QUẢ TỪ VNPAY (Callback)
        public async Task<IActionResult> PaymentCallback()
        {
            var response = _vnPayService.PaymentExecute(Request.Query);

            if (response == null || response.VnPayResponseCode != "00")
            {
                TempData["Error"] = "Thanh toán VNPay thất bại hoặc bị hủy.";
                // Cần tạo View PaymentFail hoặc redirect về trang chủ kèm thông báo
                return RedirectToAction("Index", "Home");
            }

            // Thanh toán thành công -> Cập nhật đơn hàng
            var orderId = Guid.Parse(response.OrderId);
            var order = await _context.Orders.FindAsync(orderId);

            if (order != null)
            {
                order.Status = "completed"; // Lưu ý: dùng chữ thường cho đồng bộ dữ liệu cũ
                order.PaymentMethod = "VnPay";
                order.TransactionId = response.TransactionId;
                order.PaidAt = DateTime.Now;

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Success");
        }

        // 4. TRANG THANH TOÁN THỦ CÔNG (QR Tĩnh)
        public async Task<IActionResult> Payment(Guid orderId)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return NotFound();

            return View(order);
        }

        // 5. XÁC NHẬN ĐÃ THANH TOÁN THỦ CÔNG
        public async Task<IActionResult> ConfirmPaid(Guid orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.Status = "processing";
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("PendingApproval");
        }

        // ... CÁC ACTION KHÁC (PendingApproval, Success, TrackOrder...) GIỮ NGUYÊN ...

        public IActionResult PendingApproval() => View();
        public IActionResult Success() => View();

        [HttpGet]
        public IActionResult TrackOrder() => View();

        [HttpPost]
        public async Task<IActionResult> TrackOrder(string email, string orderIdStr)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(orderIdStr))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin.";
                return View();
            }
            var userOrders = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Package)
                .Include(o => o.User)
                .Where(o => o.User.Email == email)
                .ToListAsync();

            var order = userOrders.FirstOrDefault(o =>
                o.Id.ToString().Contains(orderIdStr, StringComparison.OrdinalIgnoreCase));

            if (order == null)
            {
                ViewBag.Error = "Không tìm thấy đơn hàng.";
                return View();
            }
            return View(order);
        }

        // 7. HÀM TẢI FILE BẢO MẬT (Đã nâng cấp)
        public async Task<IActionResult> DownloadFile(Guid orderId, Guid packageId)
        {
            // 1. Lấy ID người dùng đang đăng nhập
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return RedirectToAction("Login", "Account");

            // 2. Tìm đơn hàng khớp OrderId và UserId (Chống tải trộm đơn người khác)
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Package)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == Guid.Parse(userId));

            // 3. Kiểm tra trạng thái thanh toán
            if (order == null)
            {
                return NotFound("Không tìm thấy đơn hàng của bạn.");
            }

            // Nếu đơn chưa hoàn thành -> Không cho tải
            if (order.Status != "completed")
            {
                return BadRequest("Đơn hàng chưa thanh toán thành công. Vui lòng thanh toán để tải file.");
            }

            // 4. Tìm gói sản phẩm trong đơn hàng
            var orderItem = order.OrderItems.FirstOrDefault(oi => oi.PackageId == packageId);

            if (orderItem == null || orderItem.Package == null)
            {
                return NotFound("Không tìm thấy file trong đơn hàng này.");
            }

            // 5. Xử lý đường dẫn file (Logic Bảo Mật)
            // Lấy tên file gốc từ DB (Ví dụ: /uploads/code.zip -> code.zip)
            string fileName = Path.GetFileName(orderItem.Package.FileUrl);

            // Đường dẫn đến thư mục BẢO MẬT (PrivateFiles) nằm ngoài wwwroot
            string privateFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "PrivateFiles");
            string filePath = Path.Combine(privateFolderPath, fileName);

            // 6. Kiểm tra file có tồn tại trong thư mục bảo mật không
            if (!System.IO.File.Exists(filePath))
            {
                // Fallback: Nếu bạn chưa kịp chuyển file sang PrivateFiles, thử tìm ở wwwroot cũ
                string webRootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                string publicFilePath = Path.Combine(webRootPath, orderItem.Package.FileUrl?.TrimStart('/') ?? "");

                if (System.IO.File.Exists(publicFilePath))
                {
                    filePath = publicFilePath; // Tạm thời cho tải từ nguồn cũ
                }
                else
                {
                    return NotFound("File gốc không tồn tại trên hệ thống.");
                }
            }

            // 7. Đọc file và trả về stream (Người dùng sẽ thấy trình duyệt bắt đầu tải)
            // Dùng "application/octet-stream" để trình duyệt hiểu là file tải về
            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, "application/octet-stream", fileName);
        }

        public async Task<IActionResult> History()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return RedirectToAction("Login", "Account");

            var orders = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == Guid.Parse(userId))
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Package)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == Guid.Parse(userId));

            if (order == null) return NotFound();
            return View(order);
        }
    }
}