using BanCode.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace BanCode.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context; // Khai báo DbContext

        // Inject DbContext vào Constructor
        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // 1. Lấy danh mục (Giữ nguyên)
            var categories = await _context.Categories.ToListAsync();

            // 2. Lấy sản phẩm nổi bật (Giữ nguyên logic cũ)
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductPackages)
                .Where(p => p.Status == "published")
                .OrderByDescending(p => p.CreatedAt)
                .Take(8)
                .Select(p => new ProductViewModel
                {
                    Id = p.Id,
                    Title = p.Title,
                    Slug = p.Slug,
                    ThumbnailUrl = p.ThumbnailUrl,
                    CategoryName = p.Category.Name,
                    TechStack = p.TechStack,
                    Price = p.ProductPackages.FirstOrDefault(pkg => pkg.PackageType == "basic").Price,
                    SalePrice = p.ProductPackages.FirstOrDefault(pkg => pkg.PackageType == "basic").SalePrice ?? 0,
                    Rating = 5,
                    ReviewCount = 12 // Giả lập số lượng review
                })
                .ToListAsync();

            // 3. Tạo dữ liệu đánh giá (Mock Data - Để tăng uy tín ngay lập tức)
            var testimonials = new List<TestimonialViewModel>
    {
        new TestimonialViewModel { CustomerName = "Nguyễn Văn An", CustomerRole = "Sinh viên ĐH Bách Khoa", Comment = "Code chạy ngay lần đầu, báo cáo rất chi tiết giúp mình hiểu luồng đi của dữ liệu. Cảm ơn shop!", Rating = 5, AvatarUrl = "https://ui-avatars.com/api/?name=Nguyen+Van+An&background=0D8ABC&color=fff" },
        new TestimonialViewModel { CustomerName = "Trần Thị Bích", CustomerRole = "Lập trình viên Freelance", Comment = "Mình mua bộ code bán hàng về custom lại cho khách, tiết kiệm được 2 tuần code backend. Rất đáng tiền.", Rating = 5, AvatarUrl = "https://ui-avatars.com/api/?name=Tran+Thi+Bich&background=random" },
        new TestimonialViewModel { CustomerName = "Lê Hoàng", CustomerRole = "Sinh viên K14 FPT", Comment = "Support nhiệt tình, mình bị lỗi SQL version mà admin teamview sửa giúp lúc 11h đêm. 10 điểm!", Rating = 5, AvatarUrl = "https://ui-avatars.com/api/?name=Le+Hoang&background=random" }
    };

            var model = new HomeViewModel
            {
                Categories = categories,
                FeaturedProducts = products,
                Testimonials = testimonials,
                TotalDownloads = 1540, // Con số "biết nói"
                TotalStudents = 980,
                TotalProducts = products.Count + 50
            };

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}