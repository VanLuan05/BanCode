using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using BanCode.Models;

namespace BanCode.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public AccountController(UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // --- ĐOẠN CẦN KIỂM TRA KỸ ---
                var user = new User
                {
                    UserName = model.Email, // 1. Bắt buộc phải có dòng này (Lấy Email làm tên đăng nhập)
                    Email = model.Email,    // 2. Bắt buộc phải có dòng này (Để Identity xác thực Email)
                    FullName = model.FullName,
                    CreatedAt = DateTime.Now,
                    Role = "customer"
                };
                // ----------------------------

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }

                // Nếu lỗi, hiển thị lỗi ra để biết (Ví dụ: Mật khẩu yếu, Email trùng...)
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, isPersistent: true, lockoutOnFailure: false);
                if (result.Succeeded) return RedirectToAction("Index", "Home");

                ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");
            }
            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}