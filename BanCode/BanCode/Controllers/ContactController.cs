using Microsoft.AspNetCore.Mvc;
using BanCode.Models; // Nơi chứa Contact và ContactViewModel
// using BanCode.Data; // Nếu DbContext của bạn nằm trong namespace Data

namespace BanCode.Controllers
{
    public class ContactController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ContactController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Hiển thị form liên hệ
        public IActionResult Index()
        {
            return View();
        }

        // 2. Xử lý khi khách bấm nút Gửi
        [HttpPost]
        public async Task<IActionResult> SendMessage(ContactViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Mapping: Chuyển dữ liệu từ ViewModel sang Entity để lưu
                var contact = new Contact
                {
                    Id = Guid.NewGuid(),
                    FullName = model.FullName,
                    Email = model.Email,
                    Subject = model.Subject,
                    Message = model.Message,
                    CreatedAt = DateTime.Now,
                    IsRead = false // Mặc định là chưa đọc
                };

                _context.Contacts.Add(contact);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Cảm ơn bạn! Yêu cầu hỗ trợ đã được gửi thành công.";
                return RedirectToAction("Index");
            }

            // Nếu nhập sai thì trả lại form để nhập lại
            return View("Index", model);
        }
    }
}