using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Globalization;
using Microsoft.AspNetCore.Http; // Nhớ thêm dòng này để dùng HttpContext

namespace BanCode.Services.VnPay
{
    public class VnPayLibrary
    {
        // ... (Giữ nguyên phần code class VnPayLibrary cũ) ...
        private readonly SortedList<string, string> _requestData = new SortedList<string, string>(new VnPayCompare());
        private readonly SortedList<string, string> _responseData = new SortedList<string, string>(new VnPayCompare());

        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value)) _requestData.Add(key, value);
        }

        public void AddResponseData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value)) _responseData.Add(key, value);
        }

        public string GetResponseData(string key)
        {
            return _responseData.TryGetValue(key, out var retValue) ? retValue : string.Empty;
        }

        public string CreateRequestUrl(string baseUrl, string vnp_HashSecret)
        {
            StringBuilder data = new StringBuilder();
            foreach (KeyValuePair<string, string> kv in _requestData)
            {
                if (data.Length > 0) data.Append('&');
                data.Append(kv.Key + "=" + WebUtility.UrlEncode(kv.Value));
            }
            string queryString = data.ToString();
            string vnp_SecureHash = Utils.HmacSHA512(vnp_HashSecret, queryString);
            return baseUrl + "?" + queryString + "&vnp_SecureHash=" + vnp_SecureHash;
        }

        public bool ValidateSignature(string inputHash, string vnp_HashSecret)
        {
            StringBuilder data = new StringBuilder();
            foreach (KeyValuePair<string, string> kv in _responseData)
            {
                if (kv.Key != "vnp_SecureHash" && kv.Key != "vnp_SecureHashType")
                {
                    if (data.Length > 0) data.Append('&');
                    data.Append(kv.Key + "=" + WebUtility.UrlEncode(kv.Value));
                }
            }
            string checkSum = Utils.HmacSHA512(vnp_HashSecret, data.ToString());
            return checkSum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
        }
    }

    public class VnPayCompare : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            var vnpCompare = CompareInfo.GetCompareInfo("en-US");
            return vnpCompare.Compare(x, y, CompareOptions.Ordinal);
        }
    }

    // --- GỘP CHUNG CLASS UTILS TẠI ĐÂY ---
    public static class Utils
    {
        // Hàm cũ: Mã hóa
        public static string HmacSHA512(string key, string inputData)
        {
            var hash = new StringBuilder();
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                byte[] hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }
            return hash.ToString();
        }

        // Hàm mới thêm vào: Lấy IP
        public static string GetIpAddress(HttpContext context)
        {
            var ipAddress = string.Empty;
            try
            {
                var remoteIpAddress = context.Connection.RemoteIpAddress;
                if (remoteIpAddress != null)
                {
                    if (remoteIpAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                    {
                        remoteIpAddress = System.Net.Dns.GetHostEntry(remoteIpAddress).AddressList
                            .FirstOrDefault(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                    }
                    ipAddress = remoteIpAddress.ToString();
                }
            }
            catch (Exception ex)
            {
                return "Invalid IP:" + ex.Message;
            }
            return ipAddress;
        }
    }
}