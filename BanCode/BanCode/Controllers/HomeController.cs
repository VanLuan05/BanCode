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

        public async Task<IActionResult> Index(string? keyword)
        {
            // 1. Lấy danh mục (Giữ nguyên)
            var categories = await _context.Categories.ToListAsync();

            // 2. XÂY DỰNG QUERY LẤY SẢN PHẨM
            var productsQuery = _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductPackages)
                .Where(p => p.Status == "published")
                .AsQueryable(); // Chuyển sang IQueryable để nối chuỗi điều kiện

            // --- XỬ LÝ TÌM KIẾM ---
            if (!string.IsNullOrEmpty(keyword))
            {
                string search = keyword.ToLower();
                productsQuery = productsQuery.Where(p =>
                    p.Title.ToLower().Contains(search) ||
                    p.Slug.ToLower().Contains(search) ||
                    p.TechStack.ToLower().Contains(search)
                );

                // Lưu từ khóa để hiển thị lại trên View
                ViewBag.Keyword = keyword;
            }
            // ----------------------

            // Sắp xếp mặc định
            productsQuery = productsQuery.OrderByDescending(p => p.CreatedAt);

            // LOGIC HIỂN THỊ: 
            // - Nếu KHÔNG tìm kiếm: Chỉ lấy 8 sản phẩm mới nhất (Featured)
            // - Nếu CÓ tìm kiếm: Lấy hết kết quả (hoặc giới hạn 20-50 tùy bạn)
            if (string.IsNullOrEmpty(keyword))
            {
                productsQuery = productsQuery.Take(8);
            }

            // 3. THỰC HIỆN QUERY VÀ MAPPING (Giữ nguyên logic ProductViewModel của bạn)
            var products = await productsQuery
                .Select(p => new ProductViewModel
                {
                    Id = p.Id,
                    Title = p.Title,
                    Slug = p.Slug,
                    ThumbnailUrl = p.ThumbnailUrl,
                    CategoryName = p.Category.Name,
                    ReviewCount = 15,

                    // Lấy giá
                    Price = p.ProductPackages.Where(pkg => pkg.PackageType == "basic").Select(pkg => pkg.Price).FirstOrDefault(),
                    SalePrice = p.ProductPackages.Where(pkg => pkg.PackageType == "basic").Select(pkg => pkg.SalePrice ?? pkg.Price).FirstOrDefault(),

                    // Lấy DefaultPackageId để thêm vào giỏ
                    DefaultPackageId = p.ProductPackages
                                        .Where(pkg => pkg.PackageType == "basic")
                                        .Select(pkg => pkg.Id)
                                        .FirstOrDefault()
                })
                .ToListAsync();

            // 4. Mock Data đánh giá (Giữ nguyên)
            var testimonials = new List<TestimonialViewModel>
    {
        new TestimonialViewModel { CustomerName = "Nguyễn Văn An", CustomerRole = "Sinh viên ĐH Bách Khoa", Comment = "Code chạy ngay lần đầu, báo cáo rất chi tiết giúp mình hiểu luồng đi của dữ liệu. Cảm ơn shop!", Rating = 5, AvatarUrl = "https://ui-avatars.com/api/?name=Nguyen+Van+An&background=0D8ABC&color=fff" },
        new TestimonialViewModel { CustomerName = "Trần Thị Bích", CustomerRole = "Lập trình viên Freelance", Comment = "Mình mua bộ code bán hàng về custom lại cho khách, tiết kiệm được 2 tuần code backend. Rất đáng tiền.", Rating = 5, AvatarUrl = "https://ui-avatars.com/api/?name=Tran+Thi+Bich&background=random" },
        new TestimonialViewModel { CustomerName = "Lê Hoàng", CustomerRole = "Sinh viên K14 FPT", Comment = "Support nhiệt tình, mình bị lỗi SQL version mà admin teamview sửa giúp lúc 11h đêm. 10 điểm!", Rating = 5, AvatarUrl = "https://ui-avatars.com/api/?name=Le+Hoang&background=random" }
    };

            var model = new HomeViewModel
            {
                Categories = categories,
                FeaturedProducts = products, // Lúc này nó chứa kết quả tìm kiếm hoặc 8 sản phẩm mới
                Testimonials = testimonials,
                TotalDownloads = 1540,
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


        // ---------------------------------------------
        // Action Xem Chi Tiết (URL: /san-pham/{slug})
        [Route("san-pham/{slug}")]
        public async Task<IActionResult> Detail(string slug)
        {
            if (string.IsNullOrEmpty(slug)) return NotFound();

            // 1. Lấy thông tin sản phẩm
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductPackages)
                // .Include(p => p.ProductImages) // Nếu bạn đã tạo bảng ProductImages
                .FirstOrDefaultAsync(p => p.Slug == slug && p.Status == "published");

            if (product == null) return NotFound();

            // 2. Lấy sản phẩm liên quan (Cùng danh mục, trừ chính nó)
            var relatedProducts = await _context.Products
                .Include(p => p.ProductPackages)
                .Include(p => p.Category)
                .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id && p.Status == "published")
                .Take(4)
                .Select(p => new ProductViewModel
                {
                    Id = p.Id,
                    Title = p.Title,
                    Slug = p.Slug,
                    ThumbnailUrl = p.ThumbnailUrl,
                    Price = p.ProductPackages.FirstOrDefault(pkg => pkg.PackageType == "basic").Price,
                    SalePrice = p.ProductPackages.FirstOrDefault(pkg => pkg.PackageType == "basic").SalePrice ?? 0,
                    CategoryName = p.Category.Name
                })
                .ToListAsync();

            // 3. Parse công nghệ từ JSON
            List<string> techStackList = new List<string>();
            if (!string.IsNullOrEmpty(product.TechStack))
            {
                try
                {
                    techStackList = System.Text.Json.JsonSerializer.Deserialize<List<string>>(product.TechStack);
                }
                catch { /* Bỏ qua lỗi nếu JSON sai */ }
            }

            // 4. Đổ dữ liệu vào ViewModel
            var model = new ProductDetailViewModel
            {
                Id = product.Id,
                Title = product.Title,
                Slug = product.Slug,
                Description = product.ShortDescription, // Hoặc Description tùy tên cột DB của bạn
                Content = product.Content,
                CategoryName = product.Category.Name,
                TechStack = techStackList,
                ThumbnailUrl = product.ThumbnailUrl,
                ImageGallery = new List<string> { product.ThumbnailUrl }, // Tạm thời lấy thumbnail làm gallery
                Packages = product.ProductPackages.OrderBy(p => p.Price).ToList(),
                Rating = 5, // Hardcode tạm, sau này tính trung bình từ bảng Reviews
                ReviewCount = 15,
                RelatedProducts = relatedProducts
            };

            return View(model);
        }
        // Trang hướng dẫn sử dụng
        public IActionResult Instruction()
        {
            return View();
        }
    }
}