using BanCode.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BanCode.Controllers
{
    [Authorize(Roles = "admin")]
    public class AdminProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AdminProductController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // 1. DANH SÁCH SẢN PHẨM
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            return View(products);
        }

        // 2. TRANG THÊM MỚI (GET)
        public IActionResult Create()
        {
            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");
            return View();
        }

        // 3. XỬ LÝ THÊM MỚI (POST)
        [HttpPost]
        public async Task<IActionResult> Create(AdminProductViewModel model)
        {
            if (ModelState.IsValid)
            {
                // A. XỬ LÝ UPLOAD ẢNH
                string thumbnailPath = "";
                if (model.ThumbnailImage != null)
                {
                    // Tạo tên file độc nhất để tránh trùng
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.ThumbnailImage.FileName);
                    string uploadPath = Path.Combine(_env.WebRootPath, "uploads/images");

                    // Tạo thư mục nếu chưa có
                    if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                    // Lưu file
                    using (var stream = new FileStream(Path.Combine(uploadPath, fileName), FileMode.Create))
                    {
                        await model.ThumbnailImage.CopyToAsync(stream);
                    }
                    thumbnailPath = "/uploads/images/" + fileName;
                }

                // B. XỬ LÝ UPLOAD SOURCE CODE
                string sourceCodePath = "";
                if (model.SourceCodeFile != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.SourceCodeFile.FileName);
                    string uploadPath = Path.Combine(_env.WebRootPath, "uploads/code");
                    if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                    using (var stream = new FileStream(Path.Combine(uploadPath, fileName), FileMode.Create))
                    {
                        await model.SourceCodeFile.CopyToAsync(stream);
                    }
                    sourceCodePath = "/uploads/code/" + fileName;
                }

                // C. LƯU VÀO DATABASE
                // 1. Tạo Product
                var product = new Product
                {
                    Id = Guid.NewGuid(),
                    Title = model.Title,
                    Slug = model.Slug,
                    CategoryId = model.CategoryId,
                    ThumbnailUrl = thumbnailPath,
                    // Chuyển chuỗi "C#, SQL" thành JSON ["C#", "SQL"]
                    TechStack = string.IsNullOrEmpty(model.TechStack) ? "[]" : System.Text.Json.JsonSerializer.Serialize(model.TechStack.Split(',')),
                    Status = "published",
                    CreatedAt = DateTime.Now
                };
                _context.Products.Add(product);

                // 2. Tạo Gói Basic
                var package = new ProductPackage
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    PackageType = "basic",
                    Price = model.Price,
                    SalePrice = model.SalePrice,
                    FileUrl = sourceCodePath,
                    IsActive = true
                };
                _context.ProductPackages.Add(package);

                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            // Nếu lỗi form, load lại danh mục
            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");
            return View(model);
        }
    }
}