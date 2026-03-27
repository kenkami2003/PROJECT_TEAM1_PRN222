using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using BoardingHouseManagement.Models;

namespace BoardingHouseManagement.Services.Payment
{
    public interface IPaymentService
    {
        Task<CreateOrGetPaymentResultDto> CreateOrGetPaymentAsync(Guid invoiceId);

        Task<PaymentDto> CustomerMarkPaidAsync(Guid paymentId);

        Task<PaymentDto> AdminConfirmAsync(Guid paymentId);

        Task<PaymentDto> AdminRejectAsync(Guid paymentId, string? note);

        Task<List<WaitingConfirmationPaymentDto>> GetWaitingConfirmationsAsync();

        Task<List<TenantInvoicePaymentItemDto>> GetTenantInvoicesAsync(Guid tenantId);
    }
}

