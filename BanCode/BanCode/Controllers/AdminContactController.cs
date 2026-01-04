using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BanCode.Models;

namespace BanCode.Controllers
{
    [Authorize(Roles = "admin")] // Chỉ Admin mới vào được
    public class AdminContactController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminContactController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Danh sách tin nhắn (Mới nhất lên đầu)
        public async Task<IActionResult> Index()
        {
            var messages = await _context.Contacts
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
            return View(messages);
        }

        // 2. Xem chi tiết & Đánh dấu đã đọc
        public async Task<IActionResult> Details(Guid id)
        {
            var contact = await _context.Contacts.FindAsync(id);
            if (contact == null) return NotFound();

            // Nếu chưa đọc thì đánh dấu là Đã đọc
            if (!contact.IsRead)
            {
                contact.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return View(contact);
        }

        // 3. Xóa tin nhắn rác
        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            var contact = await _context.Contacts.FindAsync(id);
            if (contact != null)
            {
                _context.Contacts.Remove(contact);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa tin nhắn.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}