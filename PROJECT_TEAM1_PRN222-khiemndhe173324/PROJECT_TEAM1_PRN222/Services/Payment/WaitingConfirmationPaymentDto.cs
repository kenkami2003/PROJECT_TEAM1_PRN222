using System;

namespace BoardingHouseManagement.Services.Payment
{
    public class WaitingConfirmationPaymentDto
    {
        public Guid PaymentId { get; init; }
        public Guid InvoiceId { get; init; }
        public string InvoiceCode { get; init; } = string.Empty;
        public string TenantName { get; init; } = string.Empty;
        public decimal Amount { get; init; }
        public string ProviderOrderCode { get; init; } = string.Empty;
        public string QrContent { get; init; } = string.Empty;
        public DateTime CustomerMarkedPaidAt { get; init; }
    }
}

