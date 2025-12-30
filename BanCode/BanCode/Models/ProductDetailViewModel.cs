namespace BanCode.Models
{
    public class ProductDetailViewModel
    {
        // Thông tin chính
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Slug { get; set; }
        public string Description { get; set; } // Mô tả ngắn
        public string Content { get; set; }     // Nội dung chi tiết (HTML)
        public string CategoryName { get; set; }
        public List<string> TechStack { get; set; } // Danh sách công nghệ

        // Hình ảnh
        public string ThumbnailUrl { get; set; }
        public List<string> ImageGallery { get; set; } // Album ảnh

        // Các gói giá (Basic, Pro...)
        public List<ProductPackage> Packages { get; set; }

        // Đánh giá
        public double Rating { get; set; }
        public int ReviewCount { get; set; }

        // Sản phẩm liên quan
        public List<ProductViewModel> RelatedProducts { get; set; }
    }
}