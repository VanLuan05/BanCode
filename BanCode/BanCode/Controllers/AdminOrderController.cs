using BanCode.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BanCode.Controllers
{
    // [Authorize(Roles = "Admin")] // Sau này nhớ bật cái này lên
    public class AdminOrderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminOrderController(ApplicationDbContext context)
        {
            _context = context;
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
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            order.Status = "completed"; // Đổi trạng thái thành công
            order.PaidAt = DateTime.Now;

            await _context.SaveChangesAsync();

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