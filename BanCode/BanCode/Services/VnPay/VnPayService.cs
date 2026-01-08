using BanCode.Models;

namespace BanCode.Services.VnPay
{
    public class VnPayService : IVnPayService
    {
        private readonly IConfiguration _config;

        public VnPayService(IConfiguration config)
        {
            _config = config;
        }

        public string CreatePaymentUrl(HttpContext context, Order order)
        {
            var tick = DateTime.Now.Ticks.ToString();
            var vnpay = new VnPayLibrary();
            var urlCallBack = _config["VnPay:ReturnUrl"];

            // 1. Điền cứng mã Website (TmnCode)
            string tmnCode = "0WVTT2G5";

            // 2. Điền cứng Mã bí mật (HashSecret)
            string hashSecret = "OCWSRMC92OMBDWPYZH47L87LIJRNGD9G";

            // 3. Điền cứng URL Sandbox
            string baseUrl = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";

            // --- Bắt đầu gán dữ liệu ---
            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", tmnCode); // Dùng biến vừa tạo
            vnpay.AddRequestData("vnp_Amount", ((long)order.TotalAmount * 100).ToString());
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", Utils.GetIpAddress(context));
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang:" + order.Id);
            vnpay.AddRequestData("vnp_OrderType", "other");

            // Lưu ý: ReturnUrl vẫn phải chính xác port 7063
            vnpay.AddRequestData("vnp_ReturnUrl", "https://localhost:7063/Order/PaymentCallback");

            vnpay.AddRequestData("vnp_TxnRef", order.Id.ToString());

            // Tạo URL
            return vnpay.CreateRequestUrl(baseUrl, hashSecret);
        }

        public PaymentResponseModel PaymentExecute(IQueryCollection collections)
        {
            var vnpay = new VnPayLibrary();
            foreach (var (key, value) in collections)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    vnpay.AddResponseData(key, value.ToString());
                }
            }

            var vnp_SecureHash = collections.FirstOrDefault(p => p.Key == "vnp_SecureHash").Value;
            var vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
            var vnp_OrderInfo = vnpay.GetResponseData("vnp_OrderInfo");
            bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, _config["VnPay:HashSecret"]);

            if (!checkSignature)
            {
                return new PaymentResponseModel { Success = false };
            }

            return new PaymentResponseModel
            {
                Success = true,
                PaymentMethod = "VnPay",
                OrderDescription = vnp_OrderInfo,
                OrderId = vnpay.GetResponseData("vnp_TxnRef"),
                TransactionId = vnpay.GetResponseData("vnp_TransactionNo"),
                Token = vnpay.GetResponseData("vnp_SecureHash"),
                VnPayResponseCode = vnp_ResponseCode
            };
        }
    }

    // ĐÃ XÓA CLASS UTILS Ở ĐÂY ĐỂ TRÁNH TRÙNG LẶP
}