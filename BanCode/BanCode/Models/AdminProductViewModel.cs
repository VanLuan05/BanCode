using System.ComponentModel.DataAnnotations;

namespace BanCode.Models
{
    public class AdminProductViewModel
    {
        public Guid Id { get; set; } // Thêm dòng này (để biết đang sửa ai)

        public string? CurrentThumbnailUrl { get; set; } // Thêm dòng này (để hiện ảnh cũ)
        // Thông tin Sản phẩm
        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
        public string? Title { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Slug (URL)")]
        public string? Slug { get; set; } // Vd: quan-ly-cafe-csharp

        public string? Description { get; set; }

        [Display(Name = "Danh mục")]
        public int CategoryId { get; set; }

        [Display(Name = "Công nghệ (cách nhau bởi dấu phẩy)")]
        public string? TechStack { get; set; } // Vd: C#, Winform, SQL

        // File Ảnh Đại Diện
        [Display(Name = "Ảnh đại diện")]
        public IFormFile? ThumbnailImage { get; set; }

        // Thông tin Gói Basic (Gói mặc định)
        [Display(Name = "Giá gốc")]
        public decimal Price { get; set; }

        [Display(Name = "Giá khuyến mãi")]
        public decimal SalePrice { get; set; }

        // File Source Code (.zip)
        [Display(Name = "File Source Code (.zip)")]
        public IFormFile? SourceCodeFile { get; set; }
    }
}