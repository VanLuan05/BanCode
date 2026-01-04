using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BanCode.Models;

namespace BanCode.Controllers
{
   
    [Authorize(Roles = "admin")] // Chỉ cho phép user có role="admin" truy cập
    public class AdminUserController : Controller
    {
        private readonly UserManager<User> _userManager;

        public AdminUserController(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        // 1. Danh sách người dùng
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
            return View(users);
        }

        // 2. Khóa tài khoản (Vĩnh viễn hoặc có thời hạn)
        [HttpPost]
        public async Task<IActionResult> LockUser(Guid id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return NotFound();

            // Khóa đến năm 9999 (Vĩnh viễn)
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

            TempData["Success"] = $"Đã khóa tài khoản {user.Email}";
            return RedirectToAction("Index");
        }

        // 3. Mở khóa tài khoản
        [HttpPost]
        public async Task<IActionResult> UnlockUser(Guid id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return NotFound();

            // Set thời gian khóa về null -> Mở ngay lập tức
            await _userManager.SetLockoutEndDateAsync(user, null);

            TempData["Success"] = $"Đã mở khóa tài khoản {user.Email}";
            return RedirectToAction("Index");
        }
    }
}