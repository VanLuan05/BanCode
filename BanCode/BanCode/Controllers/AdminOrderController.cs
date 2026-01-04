using BanCode.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BanCode.Services;
namespace BanCode.Controllers
{
    // [Authorize(Roles = "Admin")] // Sau này nhớ bật cái này lên
    public class AdminOrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;

        public AdminOrderController(ApplicationDbContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        // 1. DANH SÁCH ĐƠN HÀNG
        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .Include(o => o.User) // Lấy thông tin người mua
                .OrderByDescending(o => o.CreatedAt) // Mới nhất lên đầu
                .ToListAsync();
            return View(orders);
        }

        // 2. XEM CHI TIẾT ĐƠN (Để biết mua cái gì)
        public async Task<IActionResult> Details(Guid id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product) // Join lồng để lấy tên sản phẩm
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();
            return View(order);
        }

        // 3. DUYỆT ĐƠN (Action quan trọng nhất)
        [HttpPost]
        public async Task<IActionResult> ApproveOrder(Guid id)
        {
            // Cần Include User để lấy Email người mua
            var order = await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            // Cập nhật trạng thái
            order.Status = "completed";
            order.PaidAt = DateTime.Now;
            await _context.SaveChangesAsync();

            // --- GỬI EMAIL THÔNG BÁO ---
            if (order.User != null && !string.IsNullOrEmpty(order.User.Email))
            {
                try
                {
                    // Tạo nội dung Email
                    string orderCode = order.Id.ToString(); // Mã đơn hàng (Guid)
                    string trackingUrl = Url.Action("TrackOrder", "Order", null, Request.Scheme);

                    string subject = $"[BanCode] Đơn hàng #{orderCode.Substring(0, 8)} đã được duyệt thành công!";

                    string content = $@"
                        <div style='font-family: Arial, sans-serif; line-height: 1.6;'>
                            <h2 style='color: #0d6efd;'>Cảm ơn bạn đã mua hàng tại CodeCart!</h2>
                            <p>Xin chào <strong>{order.User.FullName}</strong>,</p>
                            <p>Đơn hàng của bạn đã được Admin xác nhận thanh toán thành công.</p>
                            
                            <div style='background-color: #f8f9fa; padding: 15px; border-left: 5px solid #198754; margin: 20px 0;'>
                                <p style='margin: 0;'>Mã đơn hàng của bạn là:</p>
                                <h3 style='margin: 5px 0; color: #dc3545;'>{orderCode}</h3>
                                <p style='margin: 0; font-size: 0.9em; color: #6c757d;'>(Hãy lưu mã này để tải lại file khi cần)</p>
                            </div>

                            <p>Bạn vui lòng truy cập đường dẫn dưới đây, nhập <strong>Email</strong> và <strong>Mã đơn hàng</strong> để tải Source Code:</p>
                            <p>
                                <a href='{trackingUrl}' style='background-color: #198754; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; font-weight: bold;'>
                                    👉 TRA CỨU & TẢI SOURCE CODE TẠI ĐÂY
                                </a>
                            </p>
                            <p>Hoặc truy cập link: {trackingUrl}</p>
                            <hr/>
                            <small>Nếu cần hỗ trợ, vui lòng liên hệ Zalo Admin.</small>
                        </div>
                    ";

                    await _emailSender.SendEmailAsync(order.User.Email, subject, content);
                }
                catch (Exception ex)
                {
                    // Log lỗi nếu gửi mail thất bại, nhưng không làm crash web
                    Console.WriteLine("Lỗi gửi mail: " + ex.Message);
                }
            }

            // TODO: Tại đây bạn có thể viết thêm code gửi Email chứa link tải cho khách

            return RedirectToAction(nameof(Details), new { id = id });
        }

        // 4. HỦY ĐƠN (Nếu khách spam hoặc không chuyển tiền)
        [HttpPost]
        public async Task<IActionResult> CancelOrder(Guid id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                order.Status = "cancelled";
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}