namespace BanCode.Models
{
    public class AdminDashboardViewModel
    {
        public decimal TotalRevenue { get; set; }      // Tổng doanh thu
        public int TotalOrders { get; set; }           // Tổng số đơn hàng
        public int TotalProducts { get; set; }         // Tổng sản phẩm
        public int TotalUsers { get; set; }            // Tổng thành viên
        public List<Order> RecentOrders { get; set; }  // Danh sách đơn mới nhất

        // Dữ liệu cho biểu đồ (Chart)
        public List<string> ChartLabels { get; set; }  // Nhãn (Ngày)
        public List<decimal> ChartData { get; set; }   // Dữ liệu (Doanh thu)
    }
}