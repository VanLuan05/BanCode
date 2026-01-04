using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BanCode.Models;

namespace BanCode.Controllers
{
    [Authorize(Roles = "admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Trang Dashboard Tổng quan
        public async Task<IActionResult> Index()
        {
            // 1. Thống kê số liệu
            var totalRevenue = await _context.Orders.Where(o => o.Status == "completed").SumAsync(o => o.TotalAmount);
            var totalOrders = await _context.Orders.CountAsync();
            var totalProducts = await _context.Products.CountAsync();
            var totalUsers = await _context.Users.CountAsync();

            // 2. Lấy 5 đơn hàng mới nhất
            var recentOrders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .ToListAsync();

            // 3. Dữ liệu biểu đồ (7 ngày gần nhất)
            var sevenDaysAgo = DateTime.Today.AddDays(-6);
            var revenueData = await _context.Orders
                .Where(o => o.Status == "completed" && o.PaidAt >= sevenDaysAgo)
                .GroupBy(o => o.PaidAt.Value.Date)
                .Select(g => new { Date = g.Key, Total = g.Sum(x => x.TotalAmount) })
                .ToListAsync();

            var chartLabels = new List<string>();
            var chartValues = new List<decimal>();

            for (int i = 0; i < 7; i++)
            {
                var date = sevenDaysAgo.AddDays(i);
                var record = revenueData.FirstOrDefault(r => r.Date == date);
                chartLabels.Add(date.ToString("dd/MM"));
                chartValues.Add(record?.Total ?? 0);
            }

            var viewModel = new AdminDashboardViewModel
            {
                TotalRevenue = totalRevenue,
                TotalOrders = totalOrders,
                TotalProducts = totalProducts,
                TotalUsers = totalUsers,
                RecentOrders = recentOrders,
                ChartLabels = chartLabels,
                ChartData = chartValues
            };

            return View(viewModel);
        }
    }
}