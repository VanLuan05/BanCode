using BanCode.Models;
using BanCode.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BanCode.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. XEM GIỎ HÀNG
        public IActionResult Index()
        {
            var cart = HttpContext.Session.Get<List<CartItem>>("Cart") ?? new List<CartItem>();
            return View(cart);
        }

        // 2. THÊM VÀO GIỎ (Đã nâng cấp: Ở lại trang cũ)
        public async Task<IActionResult> AddToCart(Guid packageId)
        {
            // --- Logic thêm vào giỏ (Giữ nguyên) ---
            var package = await _context.ProductPackages
                .Include(p => p.Product)
                .FirstOrDefaultAsync(p => p.Id == packageId);

            if (package == null) return NotFound();

            var cart = HttpContext.Session.Get<List<CartItem>>("Cart") ?? new List<CartItem>();

            if (!cart.Any(c => c.PackageId == packageId))
            {
                cart.Add(new CartItem
                {
                    PackageId = package.Id,
                    ProductId = package.ProductId,
                    ProductName = package.Product.Title,
                    PackageType = package.PackageType,
                    ThumbnailUrl = package.Product.ThumbnailUrl,
                    Price = package.SalePrice ?? package.Price
                });
                HttpContext.Session.Set("Cart", cart);
            }
            // ---------------------------------------

            // --- Logic điều hướng mới: Quay lại nơi bắt đầu ---

            // Lấy đường dẫn trang trước đó (Referer) từ Header của trình duyệt
            var referer = Request.Headers["Referer"].ToString();

            if (!string.IsNullOrEmpty(referer))
            {
                return Redirect(referer); // Quay lại trang cũ (Trang chủ hoặc Chi tiết)
            }

            // Nếu không lấy được referer (hiếm gặp), thì về trang chủ cho an toàn
            return RedirectToAction("Index", "Home");
        }

        // 3. XÓA KHỎI GIỎ
        public IActionResult Remove(Guid packageId)
        {
            var cart = HttpContext.Session.Get<List<CartItem>>("Cart") ?? new List<CartItem>();
            var item = cart.FirstOrDefault(c => c.PackageId == packageId);
            if (item != null)
            {
                cart.Remove(item);
                HttpContext.Session.Set("Cart", cart);
            }
            return RedirectToAction("Index");
        }

        // Thêm hàm này vào dưới hàm AddToCart
        // Action cho nút "THANH TOÁN NGAY"
        public async Task<IActionResult> BuyNow(Guid packageId)
        {
            // 1. Tìm sản phẩm
            var package = await _context.ProductPackages
                .Include(p => p.Product)
                .FirstOrDefaultAsync(p => p.Id == packageId);

            if (package == null) return NotFound();

            // 2. Thêm vào giỏ (nếu chưa có)
            var cart = HttpContext.Session.Get<List<CartItem>>("Cart") ?? new List<CartItem>();
            if (!cart.Any(c => c.PackageId == packageId))
            {
                cart.Add(new CartItem
                {
                    PackageId = package.Id,
                    ProductId = package.ProductId,
                    ProductName = package.Product.Title,
                    PackageType = package.PackageType,
                    ThumbnailUrl = package.Product.ThumbnailUrl,
                    Price = package.SalePrice ?? package.Price
                });
                HttpContext.Session.Set("Cart", cart);
            }

            // 3. Chuyển hướng thẳng sang trang Thanh Toán
            return RedirectToAction("Checkout", "Order");
        }
    }
}