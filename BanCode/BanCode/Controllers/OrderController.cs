using BanCode.Helpers;
using BanCode.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace BanCode.Controllers
{
    [Authorize] // Bắt buộc đăng nhập mới xem được
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        public OrderController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // 1. TRANG THANH TOÁN (GET)
        public IActionResult Checkout()
        {
            // Lấy giỏ hàng từ Session
            var cart = HttpContext.Session.Get<List<CartItem>>("Cart") ?? new List<CartItem>();

            if (cart.Count == 0)
            {
                return RedirectToAction("Index", "Cart"); // Giỏ rỗng thì đá về trang giỏ
            }

            // Truyền giỏ hàng sang View để hiển thị danh sách
            ViewBag.Cart = cart;
            ViewBag.TotalAmount = cart.Sum(item => item.Price);

            return View();
        }
        // 2. XỬ LÝ THANH TOÁN (POST)
        [HttpPost]
        public async Task<IActionResult> ProcessPayment(string fullName, string email)
        {
            // Lấy giỏ hàng
            var cart = HttpContext.Session.Get<List<CartItem>>("Cart") ?? new List<CartItem>();

            if (cart.Count == 0) return RedirectToAction("Index", "Cart");

            // 1. Tạo hoặc lấy thông tin User (Khách hàng)
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

            // 2. Tạo Đơn hàng (Order)
            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TotalAmount = cart.Sum(i => i.Price),
                Status = "pending", // Chờ thanh toán
                CreatedAt = DateTime.Now
            };
            _context.Orders.Add(order);

            // 3. Tạo Chi tiết đơn hàng (OrderItem) - Lưu từng món trong giỏ
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

            await _context.SaveChangesAsync();
            HttpContext.Session.Remove("Cart");
            // D. Chuyển hướng sang trang Quét QR
            return RedirectToAction("Payment", new { orderId = order.Id });
        }

        // 3. TRANG THANH TOÁN (Hiện QR Code)
        public async Task<IActionResult> Payment(Guid orderId)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return NotFound();

            return View(order);
        }

        // 4. GIẢ LẬP: KHÁCH ĐÃ THANH TOÁN XONG
        public async Task<IActionResult> ConfirmPaid(Guid orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                // Thay đổi: Không set completed ngay, mà giữ là pending (hoặc processing)
                // order.Status = "completed"; <--- XÓA DÒNG NÀY

                order.Status = "processing"; // Trạng thái mới: Đang xử lý
                await _context.SaveChangesAsync();
            }
            // Chuyển hướng sang trang thông báo "Đang chờ duyệt" thay vì "Thành công"
            return RedirectToAction("PendingApproval");
        }

        public IActionResult PendingApproval()
        {
            return View();
        }

        public IActionResult Success()
        {
            return View();
        }

       

        // 5. TRANG TRA CỨU ĐƠN HÀNG (GET)
        [HttpGet]
        public IActionResult TrackOrder()
        {
            return View();
        }

        // 6. XỬ LÝ TRA CỨU (POST) - ĐÃ SỬA
        [HttpPost]
        public async Task<IActionResult> TrackOrder(string email, string orderIdStr)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(orderIdStr))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin.";
                return View();
            }

            // BƯỚC 1: Tìm tất cả đơn hàng của Email này trước (Để tối ưu hiệu suất)
            // Lưu ý: Include đầy đủ thông tin để hiển thị kết quả
            var userOrders = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Package) // Nhớ là .Package nhé
                .Include(o => o.User)
                .Where(o => o.User.Email == email)
                .ToListAsync();

            // BƯỚC 2: Lọc trong danh sách đó xem có đơn nào KHỚP MÃ không
            // Dùng .ToString().Contains() để chấp nhận cả mã ngắn (8 ký tự) lẫn mã dài
            // StringComparison.OrdinalIgnoreCase để không phân biệt hoa thường
            var order = userOrders.FirstOrDefault(o =>
                o.Id.ToString().Contains(orderIdStr, StringComparison.OrdinalIgnoreCase));

            if (order == null)
            {
                ViewBag.Error = "Không tìm thấy đơn hàng hoặc Email không khớp.";
                return View();
            }

            return View(order);
        }

        // 7. HÀM TẢI FILE BẢO MẬT (Chỉ cho tải khi Status = completed)
        public async Task<IActionResult> DownloadFile(Guid orderId, Guid packageId)
        {
            // 1. Kiểm tra đơn hàng
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Package) // <-- SỬA: Đổi ProductPackage thành Package
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null || order.Status != "completed")
            {
                return BadRequest("Đơn hàng chưa được thanh toán hoặc đang chờ duyệt.");
            }

            // 2. Tìm gói sản phẩm
            var orderItem = order.OrderItems.FirstOrDefault(oi => oi.PackageId == packageId);

            // <-- SỬA: Đổi orderItem.ProductPackage thành orderItem.Package
            if (orderItem == null || orderItem.Package == null)
            {
                return NotFound("Không tìm thấy file trong đơn hàng này.");
            }

            // 3. Lấy đường dẫn file
            string webRootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            // <-- SỬA (Dòng gây lỗi 186): Đổi orderItem.ProductPackage thành orderItem.Package
            string relativePath = orderItem.Package.FileUrl?.TrimStart('/') ?? "";

            string filePath = Path.Combine(webRootPath, relativePath);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("File gốc đã bị xóa hoặc di chuyển.");
            }

            byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            string fileName = Path.GetFileName(filePath);
            return File(fileBytes, "application/octet-stream", fileName);
        }

        // 8. DANH SÁCH ĐƠN MUA
        public async Task<IActionResult> History()
        {
            // Lấy ID người dùng hiện tại
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return RedirectToAction("Login", "Account");

            // Lấy danh sách đơn hàng của user đó (kèm theo chi tiết sản phẩm để hiển thị)
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product) // Lấy tên sản phẩm
                .Where(o => o.UserId == Guid.Parse(userId))
                .OrderByDescending(o => o.CreatedAt) // Mới nhất lên đầu
                .ToListAsync();

            return View(orders);
        }

        // 9. CHI TIẾT ĐƠN HÀNG
        public async Task<IActionResult> Details(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Package) // Lấy gói để lấy FileUrl
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == Guid.Parse(userId));

            if (order == null) return NotFound();

            return View(order);
        }
    }
}