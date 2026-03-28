using System;
using System.Threading.Tasks;
using BoardingHouseManagement.Models;

namespace BoardingHouseManagement.Services
{
    public interface IPaymentService
    {
        Task<Invoice> GetInvoiceByIdAsync(Guid invoiceId);
        Task<bool> ProcessPaymentAsync(Guid invoiceId, string paymentMethod);
    }
}
