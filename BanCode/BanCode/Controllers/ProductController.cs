using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BanCode.Models;

namespace BanCode.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Action hiển thị danh sách sản phẩm (kèm tìm kiếm)
        public async Task<IActionResult> Index(string? keyword, int? categoryId, decimal? minPrice, decimal? maxPrice, string? sort)
        {
            // 1. Khởi tạo Query cơ bản
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductPackages)
                .Where(p => p.Status == "published")
                .AsQueryable();

            // 2. Lọc theo TỪ KHÓA
            if (!string.IsNullOrEmpty(keyword))
            {
                string search = keyword.ToLower();
                query = query.Where(p =>
                    p.Title.ToLower().Contains(search) ||
                    p.TechStack.ToLower().Contains(search)
                );
            }

            // 3. Lọc theo DANH MỤC
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId);
            }

            // 4. Lọc theo GIÁ (Dựa trên giá gói Basic)
            if (minPrice.HasValue)
            {
                query = query.Where(p => p.ProductPackages.Any(pkg => pkg.PackageType == "basic" && pkg.Price >= minPrice));
            }
            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.ProductPackages.Any(pkg => pkg.PackageType == "basic" && pkg.Price <= maxPrice));
            }

            // 5. SẮP XẾP
            switch (sort)
            {
                case "price_asc": // Giá tăng dần
                    query = query.OrderBy(p => p.ProductPackages.Min(pkg => pkg.Price));
                    break;
                case "price_desc": // Giá giảm dần
                    query = query.OrderByDescending(p => p.ProductPackages.Min(pkg => pkg.Price));
                    break;
                case "name_asc": // Tên A-Z
                    query = query.OrderBy(p => p.Title);
                    break;
                default: // Mặc định: Mới nhất
                    query = query.OrderByDescending(p => p.CreatedAt);
                    break;
            }

            // 6. Chuyển đổi sang ViewModel
            var products = await query
                .Select(p => new ProductViewModel
                {
                    Id = p.Id,
                    Title = p.Title,
                    Slug = p.Slug,
                    ThumbnailUrl = p.ThumbnailUrl,
                    CategoryName = p.Category.Name,
                    TechStack = p.TechStack,
                    Price = p.ProductPackages.Where(pkg => pkg.PackageType == "basic").Select(pkg => pkg.Price).FirstOrDefault(),
                    SalePrice = p.ProductPackages.Where(pkg => pkg.PackageType == "basic").Select(pkg => pkg.SalePrice ?? pkg.Price).FirstOrDefault(),
                    DefaultPackageId = p.ProductPackages.Where(pkg => pkg.PackageType == "basic").Select(pkg => pkg.Id).FirstOrDefault()
                })
                .ToListAsync();

            // 7. Chuẩn bị dữ liệu cho View
            var model = new ProductListViewModel
            {
                Products = products,
                Categories = await _context.Categories.ToListAsync(), // Lấy list danh mục để hiện bên trái
                CurrentKeyword = keyword,
                CurrentCategoryId = categoryId,
                CurrentMinPrice = minPrice,
                CurrentMaxPrice = maxPrice,
                CurrentSort = sort
            };

            return View(model);
        }
    }
}