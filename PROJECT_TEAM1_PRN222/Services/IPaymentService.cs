using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using BoardingHouseManagement.Models;

namespace BoardingHouseManagement.Services
{
    public interface IPaymentService
    {
        Task<Invoice> GetInvoiceByIdAsync(Guid invoiceId);
        Task<Payment> CreatePendingVnPayPaymentAsync(Guid invoiceId, string currentUser);
        Task<Payment> GetLatestByInvoiceIdAsync(Guid invoiceId);
        Task<(bool IsSuccess, Guid? InvoiceId)> HandleVnPayReturnAsync(IQueryCollection queryParams);
        Task<bool> MarkSuccessAsync(string txnRef, string vnpTransactionNo);
        Task<bool> MarkFailedAsync(string txnRef, string vnpResponseCode);
        Task<System.Collections.Generic.IEnumerable<BoardingHouseManagement.Models.Payment>> GetAllPaymentsAsync(BoardingHouseManagement.Models.PaymentStatus? filterStatus);
    }
}
