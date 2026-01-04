using Microsoft.AspNetCore.Mvc;
using BanCode.Models;

namespace BanCode.Controllers
{
    public class ContactController : Controller
    {
        // 1. Hiển thị trang liên hệ
        public IActionResult Index()
        {
            return View();
        }

        // 2. Xử lý khi bấm nút Gửi
        [HttpPost]
        public IActionResult SendMessage(ContactViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Ở đây bạn có thể viết code:
                // 1. Lưu vào Database (cần tạo thêm bảng Contacts)
                // 2. Hoặc gửi Email cho Admin (Dùng SMTP)

                // Tạm thời mình giả lập là đã gửi thành công
                TempData["Success"] = "Cảm ơn bạn đã liên hệ! Chúng tôi sẽ phản hồi sớm nhất.";
                return RedirectToAction("Index");
            }

            return View("Index", model);
        }
    }
}