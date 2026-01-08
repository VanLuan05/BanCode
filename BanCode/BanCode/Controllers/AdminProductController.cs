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

        [HttpPost]
        public async Task<IActionResult> Create(AdminProductViewModel model)
        {
            if (ModelState.IsValid)
            {
                // A. XỬ LÝ UPLOAD ẢNH (GIỮ NGUYÊN - Vẫn lưu ở wwwroot để hiển thị)
                string thumbnailPath = "";
                if (model.ThumbnailImage != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.ThumbnailImage.FileName);
                    string uploadPath = Path.Combine(_env.WebRootPath, "uploads/images");

                    if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                    using (var stream = new FileStream(Path.Combine(uploadPath, fileName), FileMode.Create))
                    {
                        await model.ThumbnailImage.CopyToAsync(stream);
                    }
                    thumbnailPath = "/uploads/images/" + fileName;
                }

                // B. XỬ LÝ UPLOAD SOURCE CODE (SỬA LẠI: LƯU VÀO THƯ MỤC BẢO MẬT)
                string sourceCodePath = "";
                if (model.SourceCodeFile != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.SourceCodeFile.FileName);

                    // --- THAY ĐỔI QUAN TRỌNG TẠI ĐÂY ---
                    // 1. Đường dẫn vật lý: Lưu ra ngoài wwwroot (vào thư mục PrivateFiles)
                    string privateFolder = Path.Combine(Directory.GetCurrentDirectory(), "PrivateFiles");

                    // Tạo thư mục PrivateFiles nếu chưa có
                    if (!Directory.Exists(privateFolder)) Directory.CreateDirectory(privateFolder);

                    // 2. Lưu file vào đó
                    using (var stream = new FileStream(Path.Combine(privateFolder, fileName), FileMode.Create))
                    {
                        await model.SourceCodeFile.CopyToAsync(stream);
                    }

                    // 3. Đường dẫn lưu DB: Lưu tên file (hoặc đường dẫn ảo)
                    // Lưu ý: Hàm DownloadFile ở OrderController sẽ dùng Path.GetFileName để lấy tên file này
                    sourceCodePath = "PrivateFiles/" + fileName;
                }

                // C. LƯU VÀO DATABASE (GIỮ NGUYÊN)
                var product = new Product
                {
                    Id = Guid.NewGuid(),
                    Title = model.Title,
                    Slug = model.Slug,
                    CategoryId = model.CategoryId,
                    ThumbnailUrl = thumbnailPath,
                    TechStack = string.IsNullOrEmpty(model.TechStack) ? "[]" : System.Text.Json.JsonSerializer.Serialize(model.TechStack.Split(',')),
                    Status = "published",
                    CreatedAt = DateTime.Now
                };
                _context.Products.Add(product);

                var package = new ProductPackage
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    PackageType = "basic",
                    Price = model.Price,
                    SalePrice = model.SalePrice,
                    FileUrl = sourceCodePath, // Lưu đường dẫn bảo mật
                    IsActive = true
                };
                _context.ProductPackages.Add(package);

                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }

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

        // 5. XỬ LÝ CẬP NHẬT (POST) - ĐÃ SỬA BẢO MẬT
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

                    // 2. Xử lý Ảnh mới (GIỮ NGUYÊN - Vẫn lưu public)
                    if (model.ThumbnailImage != null)
                    {
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.ThumbnailImage.FileName);
                        string uploadPath = Path.Combine(_env.WebRootPath, "uploads/images");
                        if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                        using (var stream = new FileStream(Path.Combine(uploadPath, fileName), FileMode.Create))
                        {
                            await model.ThumbnailImage.CopyToAsync(stream);
                        }

                        productToUpdate.ThumbnailUrl = "/uploads/images/" + fileName;
                    }

                    // 3. Cập nhật Giá
                    var packageToUpdate = await _context.ProductPackages
                        .FirstOrDefaultAsync(p => p.ProductId == id && p.PackageType == "basic");

                    if (packageToUpdate != null)
                    {
                        packageToUpdate.Price = model.Price;
                        packageToUpdate.SalePrice = model.SalePrice;

                        // 4. Xử lý File code mới (SỬA LẠI: LƯU VÀO PRIVATEFILES)
                        if (model.SourceCodeFile != null)
                        {
                            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.SourceCodeFile.FileName);

                            // --- THAY ĐỔI: Lưu vào thư mục bảo mật ---
                            string privateFolder = Path.Combine(Directory.GetCurrentDirectory(), "PrivateFiles");
                            if (!Directory.Exists(privateFolder)) Directory.CreateDirectory(privateFolder);

                            // Lưu file vật lý
                            using (var stream = new FileStream(Path.Combine(privateFolder, fileName), FileMode.Create))
                            {
                                await model.SourceCodeFile.CopyToAsync(stream);
                            }

                            // Cập nhật đường dẫn trong DB
                            packageToUpdate.FileUrl = "PrivateFiles/" + fileName;
                            // ------------------------------------------
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

        // 6. TRANG XÁC NHẬN XÓA (GET)
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null) return NotFound();

            return View(product);
        }

        // 7. XỬ LÝ XÓA (POST) - ĐÃ CẬP NHẬT LOGIC XÓA FILE BẢO MẬT
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            // 1. Kiểm tra xem sản phẩm đã có ai mua chưa
            var hasOrders = await _context.OrderItems.AnyAsync(oi => oi.ProductId == id);
            if (hasOrders)
            {
                TempData["Error"] = "Không thể xóa sản phẩm này vì đã có đơn hàng liên quan. Bạn chỉ có thể chuyển trạng thái sang 'Ngừng kinh doanh' (Draft).";
                return RedirectToAction(nameof(Index));
            }

            // 2. Lấy thông tin sản phẩm và các gói liên quan
            var product = await _context.Products
                .Include(p => p.ProductPackages) // Quan trọng: lấy gói để biết đường dẫn file
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product != null)
            {
                // 3. Xóa File ảnh Thumbnail (Vẫn xóa ở wwwroot như cũ)
                if (!string.IsNullOrEmpty(product.ThumbnailUrl))
                {
                    // Xử lý đường dẫn tương đối (bỏ dấu / ở đầu nếu có)
                    string webRootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    string imgPath = Path.Combine(webRootPath, product.ThumbnailUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imgPath)) System.IO.File.Delete(imgPath);
                }

                // 4. Xóa File Source Code (SỬA LẠI: XÓA TRONG PRIVATEFILES)
                foreach (var package in product.ProductPackages)
                {
                    if (!string.IsNullOrEmpty(package.FileUrl))
                    {
                        // Logic cũ: Xóa ở wwwroot (Sai với hệ thống mới)
                        // string filePath = Path.Combine(_env.WebRootPath, package.FileUrl.TrimStart('/'));

                        // --- Logic Mới: Xóa ở PrivateFiles hoặc wwwroot (để hỗ trợ cả file cũ lẫn mới) ---

                        string filePath;

                        // Kiểm tra xem đường dẫn có bắt đầu bằng "PrivateFiles" hay không
                        if (package.FileUrl.StartsWith("PrivateFiles"))
                        {
                            // Đây là file bảo mật mới
                            filePath = Path.Combine(Directory.GetCurrentDirectory(), package.FileUrl);
                        }
                        else
                        {
                            // Đây là file cũ (lúc chưa nâng cấp bảo mật) nằm trong wwwroot
                            string webRootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                            filePath = Path.Combine(webRootPath, package.FileUrl.TrimStart('/'));
                        }

                        // Thực hiện xóa vật lý
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }
                    }
                }

                // 5. Xóa dữ liệu trong Database
                _context.ProductPackages.RemoveRange(product.ProductPackages);
                _context.Reviews.RemoveRange(product.Reviews);
                _context.Products.Remove(product);

                await _context.SaveChangesAsync();

                TempData["Success"] = "Đã xóa sản phẩm và dọn dẹp file rác thành công.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}