
using BoardingHouseManagement.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BoardingHouseManagement.Services.Admin
{
    public interface IAdminInvoiceService
    {
        Task<IEnumerable<Invoice>> GetAllAsync(int? month = null, int? year = null, InvoiceStatus? status = null);
        Task<Invoice?> GetByIdAsync(Guid id);
        Task<bool> CreateAsync(Invoice invoice);
        Task<bool> UpdateAsync(Invoice invoice);
        Task<IEnumerable<Contract>> GetActiveContractsAsync();
        Task<BoardingHouseManagement.ViewModels.Admin.InvoiceSuggestionVM> GetInvoiceSuggestionAsync(Guid contractId, int month, int year);
    }
}
