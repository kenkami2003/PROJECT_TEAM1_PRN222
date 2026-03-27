using System;
using BoardingHouseManagement.Models;

namespace BoardingHouseManagement.Services.Payment
{
    public class TenantInvoicePaymentItemDto
    {
        public Guid InvoiceId { get; init; }
        public string InvoiceCode { get; init; } = string.Empty;
        public decimal TotalAmount { get; init; }
        public InvoiceStatus InvoiceStatus { get; init; }
        public int Month { get; init; }
        public int Year { get; init; }
    }
}

