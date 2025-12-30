using BanCode.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
namespace BanCode.Controllers
{
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        public OrderController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // 1. TRANG CHECKOUT (Xem lại đơn hàng & Điền thông tin)
        [HttpGet]
        public async Task<IActionResult> Checkout(Guid packageId)
        {
            var package = await _context.ProductPackages
                .Include(p => p.Product)
                .FirstOrDefaultAsync(p => p.Id == packageId);

            if (package == null) return NotFound();

            // Truyền gói tin sang View để hiển thị
            return View(package);
        }

        // 2. XỬ LÝ THANH TOÁN (Tạo đơn hàng -> Chuyển sang trang QR)
        [HttpPost]
        public async Task<IActionResult> ProcessPayment(Guid packageId, string fullName, string email)
        {
            var package = await _context.ProductPackages.FindAsync(packageId);
            if (package == null) return NotFound();

            // A. Kiểm tra hoặc tạo User mới (Khách vãng lai)
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = email,
                    FullName = fullName,
                    Role = "customer",
                    CreatedAt = DateTime.Now
                };
                _context.Users.Add(user);
            }

            // B. Tạo Đơn hàng (Pending)
            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TotalAmount = package.SalePrice ?? package.Price,
                Status = "pending", // Chờ thanh toán
                PaymentMethod = "QR_BANK",
                CreatedAt = DateTime.Now
            };
            _context.Orders.Add(order);

            // C. Tạo Chi tiết đơn hàng
            var orderItem = new OrderItem
            {
                OrderId = order.Id,
                ProductId = package.ProductId,
                PackageId = package.Id,
                PriceAtPurchase = package.SalePrice ?? package.Price
            };
            _context.OrderItems.Add(orderItem);

            await _context.SaveChangesAsync();

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

        // ... (Các code cũ giữ nguyên)

        // 5. TRANG TRA CỨU ĐƠN HÀNG (GET)
        [HttpGet]
        public IActionResult TrackOrder()
        {
            return View();
        }

        // 6. XỬ LÝ TRA CỨU (POST)
        [HttpPost]
        public async Task<IActionResult> TrackOrder(string email, string orderIdStr)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(orderIdStr))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin.";
                return View();
            }

            // Cố gắng parse Guid, nếu lỗi thì báo sai mã
            if (!Guid.TryParse(orderIdStr, out Guid orderId))
            {
                ViewBag.Error = "Mã đơn hàng không hợp lệ.";
                return View();
            }

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product) // Để lấy tên sản phẩm
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Package) // Để lấy link tải
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.User.Email == email);

            if (order == null)
            {
                ViewBag.Error = "Không tìm thấy đơn hàng hoặc Email không khớp.";
                return View();
            }

            return View(order); // Trả về View với thông tin đơn hàng tìm được
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
    }
}