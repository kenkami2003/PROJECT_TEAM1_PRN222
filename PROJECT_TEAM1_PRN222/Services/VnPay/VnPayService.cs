using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using BoardingHouseManagement.Models;
using BoardingHouseManagement.Models.VnPay;
using BoardingHouseManagement.Helpers;

namespace BoardingHouseManagement.Services.VnPay
{
    public class VnPayService : IVnPayService
    {
        private readonly VnPayOptions _config;

        public VnPayService(IOptions<VnPayOptions> options)
        {
            _config = options.Value;
        }

        public string BuildTxnRef()
        {
            return DateTime.Now.Ticks.ToString();
        }

        public string CreatePaymentUrl(Payment payment, HttpContext httpContext)
        {
            var vnpay = new VnPayLibrary();

            vnpay.AddRequestData("vnp_Version", _config.Version);
            vnpay.AddRequestData("vnp_Command", _config.Command);
            vnpay.AddRequestData("vnp_TmnCode", _config.TmnCode);
            // VNPAY mong đợi số tiền được nhân lên 100
            vnpay.AddRequestData("vnp_Amount", ((long)(payment.Amount * 100)).ToString()); 
            vnpay.AddRequestData("vnp_CreateDate", payment.CreatedAt.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", _config.CurrCode);
            vnpay.AddRequestData("vnp_IpAddr", Utils.GetIpAddress(httpContext));
            vnpay.AddRequestData("vnp_Locale", _config.Locale);
            vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan hoa don: " + payment.InvoiceId);
            vnpay.AddRequestData("vnp_OrderType", "other"); // Code bill payment
            vnpay.AddRequestData("vnp_ReturnUrl", _config.ReturnUrl);
            vnpay.AddRequestData("vnp_TxnRef", payment.TxnRef); 

            string finalUrl = vnpay.CreateRequestUrl(_config.BaseUrl, _config.HashSecret);
            
            // Log for debugging (Critical for Code 72 analysis)
            Console.WriteLine("--- VNPAY DEBUG URL ---");
            Console.WriteLine(finalUrl);
            Console.WriteLine("-----------------------");

            return finalUrl;
        }

        public bool ValidateSignature(IQueryCollection collections)
        {
            var vnpay = new VnPayLibrary();
            foreach (var (key, value) in collections)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    vnpay.AddResponseData(key, value.ToString());
                }
            }
            string vnp_SecureHash = collections["vnp_SecureHash"].ToString();
            return vnpay.ValidateSignature(vnp_SecureHash, _config.HashSecret);
        }

        public (string vnp_ResponseCode, string vnp_TransactionNo, string vnp_TxnRef) ParseReturnResponse(IQueryCollection collections)
        {
            string vnp_ResponseCode = collections["vnp_ResponseCode"].ToString();
            string vnp_TransactionNo = collections["vnp_TransactionNo"].ToString();
            string vnp_TxnRef = collections["vnp_TxnRef"].ToString();
            
            return (vnp_ResponseCode, vnp_TransactionNo, vnp_TxnRef);
        }
    }
}
