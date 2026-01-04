namespace BanCode.Models
{
    public class CartItem
    {
        public Guid PackageId { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public string PackageType { get; set; }
        public string ThumbnailUrl { get; set; }
        public decimal Price { get; set; }
    }
}