using System;
using BoardingHouseManagement.Models;

namespace BoardingHouseManagement.Services.Invoice
{
    public class InvoiceListItemDto
    {
        public Guid Id { get; init; }
        public string InvoiceCode { get; init; } = string.Empty;
        public int Month { get; init; }
        public int Year { get; init; }
        public string TenantName { get; init; } = string.Empty;
        public string ContractCode { get; init; } = string.Empty;
        public string RoomNumber { get; init; } = string.Empty;
        public decimal TotalAmount { get; init; }
        public InvoiceStatus Status { get; init; }
        public Guid? WaitingConfirmationPaymentId { get; set; }
    }
}
