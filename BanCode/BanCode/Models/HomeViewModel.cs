using System.Collections.Generic;

namespace BanCode.Models
{
    public class ProductViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Slug { get; set; }
        public string ThumbnailUrl { get; set; }
        public decimal Price { get; set; }
        public decimal SalePrice { get; set; }
        public string CategoryName { get; set; }
        public string TechStack { get; set; }
        public double Rating { get; set; }
        public int ReviewCount { get; set; }

        // Thêm thuộc tính DefaultPackageId để xác định gói mặc định
        public Guid DefaultPackageId { get; set; }
    }

    // Class mới để chứa đánh giá khách hàng
    public class TestimonialViewModel
    {
        public string CustomerName { get; set; }
        public string CustomerRole { get; set; } // Vd: Sinh viên K18, Freelancer
        public string AvatarUrl { get; set; }
        public string Comment { get; set; }
        public int Rating { get; set; }
    }

    public class HomeViewModel
    {
        public List<Category> Categories { get; set; }
        public List<ProductViewModel> FeaturedProducts { get; set; }
        public List<TestimonialViewModel> Testimonials { get; set; } // Mới

        // Thống kê giả lập để tăng uy tín
        public int TotalDownloads { get; set; } = 1250;
        public int TotalStudents { get; set; } = 850;
        public int TotalProducts { get; set; } = 120;
    }
}