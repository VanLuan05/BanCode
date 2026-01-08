using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BanCode.Models;
using BanCode.Services; // Quan trọng: Để dùng IEmailSender

namespace BanCode.Controllers
{
    [Authorize(Roles = "admin")]
    public class AdminContactController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender; // Khai báo dịch vụ Email

        // Inject dịch vụ vào Constructor
        public AdminContactController(ApplicationDbContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        // 1. Danh sách tin nhắn
        public async Task<IActionResult> Index()
        {
            var messages = await _context.Contacts
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
            return View(messages);
        }

        // 2. Xem chi tiết
        public async Task<IActionResult> Details(Guid id)
        {
            var contact = await _context.Contacts.FindAsync(id);
            if (contact == null) return NotFound();

            if (!contact.IsRead)
            {
                contact.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return View(contact);
        }

        // 3. Xử lý Gửi phản hồi (ACTION QUAN TRỌNG NÀY ĐANG THIẾU)
        [HttpPost]
        public async Task<IActionResult> Reply(Guid id, string replyMessage)
        {
            var contact = await _context.Contacts.FindAsync(id);
            if (contact == null) return NotFound();

            if (string.IsNullOrWhiteSpace(replyMessage))
            {
                TempData["Error"] = "Nội dung trả lời không được để trống.";
                return RedirectToAction("Details", new { id = id });
            }

            // Tạo nội dung Email đẹp mắt
            string subject = $"[BanCode] Phản hồi yêu cầu: {contact.Subject}";
            string content = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h3 style='color: #0d6efd;'>Xin chào {contact.FullName},</h3>
                    <p>Cảm ơn bạn đã liên hệ với BanCode. Về vấn đề của bạn, chúng tôi xin phản hồi như sau:</p>
                    <div style='background: #f8f9fa; padding: 15px; border-left: 4px solid #0d6efd; margin: 20px 0;'>
                        {replyMessage.Replace("\n", "<br>")}
                    </div>
                    <p>Nếu cần hỗ trợ thêm, vui lòng trả lời email này.</p>
                    <hr style='border: 0; border-top: 1px solid #eee;'>
                    <p style='color: #888; font-size: 12px;'>Trân trọng,<br>Đội ngũ BanCode Support</p>
                </div>
            ";

            try
            {
                await _emailSender.SendEmailAsync(contact.Email, subject, content);
                TempData["Success"] = "Đã gửi email trả lời thành công!";
            }
            catch
            {
                TempData["Error"] = "Lỗi gửi mail. Vui lòng kiểm tra lại cấu hình SMTP trong appsettings.json";
            }

            return RedirectToAction("Details", new { id = id });
        }

        // 4. Xóa tin nhắn
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