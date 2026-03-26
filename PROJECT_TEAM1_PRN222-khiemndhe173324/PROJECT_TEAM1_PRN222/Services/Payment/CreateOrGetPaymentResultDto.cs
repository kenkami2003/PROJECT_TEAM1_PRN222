using System;

using BoardingHouseManagement.Models;

namespace BoardingHouseManagement.Services.Payment
{
    public class CreateOrGetPaymentResultDto
    {
        public Guid InvoiceId { get; init; }
        public bool InvoicePaid { get; init; }
        public string InvoiceCode { get; init; } = string.Empty;
        public string TenantName { get; init; } = string.Empty;
        public PaymentDto? Payment { get; init; }

        public int Month { get; init; }
        public int Year { get; init; }
        public decimal RoomAmount { get; init; }
        public decimal UtilityAmount { get; init; }
        public decimal ServiceAmount { get; init; }
        public decimal PenaltyAmount { get; init; }
        public decimal InvoiceTotalAmount { get; init; }

        public CreateOrGetPaymentResultDto(
            Guid invoiceId,
            bool invoicePaid,
            string invoiceCode,
            string tenantName,
            PaymentDto? payment,
            int month,
            int year,
            decimal roomAmount,
            decimal utilityAmount,
            decimal serviceAmount,
            decimal penaltyAmount,
            decimal invoiceTotalAmount)
        {
            InvoiceId = invoiceId;
            InvoicePaid = invoicePaid;
            InvoiceCode = invoiceCode;
            TenantName = tenantName;
            Payment = payment;
            Month = month;
            Year = year;
            RoomAmount = roomAmount;
            UtilityAmount = utilityAmount;
            ServiceAmount = serviceAmount;
            PenaltyAmount = penaltyAmount;
            InvoiceTotalAmount = invoiceTotalAmount;
        }

        public static CreateOrGetPaymentResultDto FromInvoice(
            BoardingHouseManagement.Models.Invoice invoice,
            bool invoicePaid,
            string tenantName,
            PaymentDto? payment)
        {
            return new CreateOrGetPaymentResultDto(
                invoice.Id,
                invoicePaid,
                invoice.InvoiceCode,
                tenantName,
                payment,
                invoice.Month,
                invoice.Year,
                invoice.RoomAmount,
                invoice.UtilityAmount,
                invoice.ServiceAmount,
                invoice.PenaltyAmount,
                invoice.TotalAmount);
        }
    }
}
