using BanCode.Models;

namespace BanCode.Models
{
    public class ProductListViewModel
    {
        public IEnumerable<ProductViewModel> Products { get; set; } // Danh sách sản phẩm
        public IEnumerable<Category> Categories { get; set; }       // Danh sách danh mục (để hiện bên sidebar)

        // Các giá trị đang lọc (để giữ lại trên form khi load trang)
        public string? CurrentKeyword { get; set; }
        public int? CurrentCategoryId { get; set; }
        public decimal? CurrentMinPrice { get; set; }
        public decimal? CurrentMaxPrice { get; set; }
        public string? CurrentSort { get; set; } // "price_asc", "price_desc", "newest"
    }
}