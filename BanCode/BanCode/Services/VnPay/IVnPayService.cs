using BanCode.Models;

namespace BanCode.Services.VnPay
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(HttpContext context, Order order);
        PaymentResponseModel PaymentExecute(IQueryCollection collections);
    }

    public class PaymentResponseModel
    {
        public bool Success { get; set; }
        public string PaymentMethod { get; set; }
        public string OrderDescription { get; set; }
        public string OrderId { get; set; }
        public string PaymentId { get; set; }
        public string TransactionId { get; set; }
        public string Token { get; set; }
        public string VnPayResponseCode { get; set; }
    }
}