using Microsoft.AspNetCore.Http;
using BoardingHouseManagement.Models;

namespace BoardingHouseManagement.Services.VnPay
{
    public interface IVnPayService
    {
        string BuildTxnRef();
        string CreatePaymentUrl(Payment payment, HttpContext httpContext);
        bool ValidateSignature(IQueryCollection collections);
        (string vnp_ResponseCode, string vnp_TransactionNo, string vnp_TxnRef) ParseReturnResponse(IQueryCollection collections);
    }
}
