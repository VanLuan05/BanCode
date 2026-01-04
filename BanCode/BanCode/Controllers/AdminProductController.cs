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

        // 4. TRANG CHỈNH SỬA (GET)
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            // Lấy sản phẩm kèm theo Gói bán (ProductPackages)
            var product = await _context.Products
                .Include(p => p.ProductPackages)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            // Lấy gói Basic để hiển thị giá
            var basicPackage = product.ProductPackages.FirstOrDefault(p => p.PackageType == "basic");

            // Chuyển JSON TechStack thành chuỗi (Ví dụ: ["C#","SQL"] -> "C#, SQL")
            string techStackString = "";
            try
            {
                if (!string.IsNullOrEmpty(product.TechStack))
                {
                    var tags = System.Text.Json.JsonSerializer.Deserialize<string[]>(product.TechStack);
                    if (tags != null) techStackString = string.Join(", ", tags);
                }
            }
            catch { /* Bỏ qua nếu lỗi JSON */ }

            // Đổ dữ liệu vào ViewModel
            var model = new AdminProductViewModel
            {
                Id = product.Id,
                Title = product.Title,
                Slug = product.Slug,
                CategoryId = product.CategoryId,
                CurrentThumbnailUrl = product.ThumbnailUrl, // Ảnh cũ
                Price = basicPackage?.Price ?? 0,
                SalePrice = (decimal)(basicPackage?.SalePrice),
                TechStack = techStackString
            };

            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(model);
        }

        // 5. XỬ LÝ CẬP NHẬT (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, AdminProductViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Lấy sản phẩm gốc từ DB
                    var productToUpdate = await _context.Products.FindAsync(id);
                    if (productToUpdate == null) return NotFound();

                    // 1. Cập nhật thông tin cơ bản
                    productToUpdate.Title = model.Title;
                    productToUpdate.Slug = model.Slug;
                    productToUpdate.CategoryId = model.CategoryId;

                    // Chuyển TechStack về JSON
                    if (!string.IsNullOrEmpty(model.TechStack))
                    {
                        var tags = model.TechStack.Split(',').Select(t => t.Trim()).ToArray();
                        productToUpdate.TechStack = System.Text.Json.JsonSerializer.Serialize(tags);
                    }

                    // 2. Xử lý Ảnh mới (Nếu có upload)
                    if (model.ThumbnailImage != null)
                    {
                        // Upload ảnh mới
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.ThumbnailImage.FileName);
                        string uploadPath = Path.Combine(_env.WebRootPath, "uploads/images");
                        if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                        using (var stream = new FileStream(Path.Combine(uploadPath, fileName), FileMode.Create))
                        {
                            await model.ThumbnailImage.CopyToAsync(stream);
                        }

                        // Xóa ảnh cũ (nếu muốn tiết kiệm dung lượng - Optional)
                        // if (!string.IsNullOrEmpty(productToUpdate.ThumbnailUrl)) ...

                        // Cập nhật đường dẫn mới
                        productToUpdate.ThumbnailUrl = "/uploads/images/" + fileName;
                    }

                    // 3. Cập nhật Giá (trong bảng ProductPackages)
                    var packageToUpdate = await _context.ProductPackages
                        .FirstOrDefaultAsync(p => p.ProductId == id && p.PackageType == "basic");

                    if (packageToUpdate != null)
                    {
                        packageToUpdate.Price = model.Price;
                        packageToUpdate.SalePrice = model.SalePrice;

                        // 4. Xử lý File code mới (Nếu có upload)
                        if (model.SourceCodeFile != null)
                        {
                            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.SourceCodeFile.FileName);
                            string uploadPath = Path.Combine(_env.WebRootPath, "uploads/code");
                            if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                            using (var stream = new FileStream(Path.Combine(uploadPath, fileName), FileMode.Create))
                            {
                                await model.SourceCodeFile.CopyToAsync(stream);
                            }
                            packageToUpdate.FileUrl = "/uploads/code/" + fileName;
                        }
                    }

                    // Lưu tất cả thay đổi
                    _context.Update(productToUpdate);
                    if (packageToUpdate != null) _context.Update(packageToUpdate);

                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Products.Any(e => e.Id == id)) return NotFound();
                    else throw;
                }
            }

            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", model.CategoryId);
            return View(model);
        }
    }
}