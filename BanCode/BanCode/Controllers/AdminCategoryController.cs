using BanCode.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BanCode.Controllers
{
    public class AdminCategoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminCategoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. DANH SÁCH DANH MỤC
        public async Task<IActionResult> Index()
        {
            var data = await _context.Categories.OrderByDescending(c => c.Id).ToListAsync();
            return View(data);
        }

        // 2. THÊM MỚI (GET - Hiển thị form)
        public IActionResult Create()
        {
            return View();
        }

        // 3. XỬ LÝ THÊM MỚI (POST - Lưu vào DB)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra trùng Slug
                if (await _context.Categories.AnyAsync(c => c.Slug == category.Slug))
                {
                    ModelState.AddModelError("Slug", "Đường dẫn (Slug) này đã tồn tại, vui lòng chọn cái khác.");
                    return View(category);
                }

                _context.Add(category);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // 4. SỬA (GET - Hiển thị form cũ)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            return View(category);
        }

        // 5. XỬ LÝ SỬA (POST - Cập nhật DB)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category category)
        {
            if (id != category.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Kiểm tra trùng Slug (trừ chính nó ra)
                    if (await _context.Categories.AnyAsync(c => c.Slug == category.Slug && c.Id != id))
                    {
                        ModelState.AddModelError("Slug", "Đường dẫn (Slug) này đã tồn tại.");
                        return View(category);
                    }

                    _context.Update(category);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Categories.Any(e => e.Id == category.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // 6. XÓA (GET - Xác nhận xóa)
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var category = await _context.Categories.FirstOrDefaultAsync(m => m.Id == id);
            if (category == null) return NotFound();

            return View(category);
        }

        // 7. XỬ LÝ XÓA (POST)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}