using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BoardingHouseManagement.Services.Invoice
{
    public interface IInvoiceAdminService
    {
        Task<List<InvoiceListItemDto>> GetInvoicesAsync();
        Task<List<ContractSelectItemDto>> GetActiveContractsForSelectAsync();
        Task<InvoiceEditDto?> GetForEditAsync(Guid id);
        Task<InvoiceEditDto> GetEmptyForCreateAsync();
        Task<Guid> CreateAsync(InvoiceEditDto input);
        Task UpdateAsync(InvoiceEditDto input);
    }
}
