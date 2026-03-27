using System;
using BoardingHouseManagement.Models;

namespace BoardingHouseManagement.Services.Payment
{
    public class PaymentDto
    {
        public Guid Id { get; init; }
        public Guid InvoiceId { get; init; }
        public decimal Amount { get; init; }
        public PaymentStatus Status { get; init; }
        public PaymentProvider Provider { get; init; }
        public string ProviderOrderCode { get; init; }
        public string QrContent { get; init; }
        public string CheckoutUrl { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? ExpiredAt { get; init; }
        public DateTime? PaidAt { get; init; }

        public static PaymentDto FromEntity(BoardingHouseManagement.Models.Payment entity)
        {
            return new PaymentDto
            {
                Id = entity.Id,
                InvoiceId = entity.InvoiceId,
                Amount = entity.Amount,
                Status = entity.Status ?? PaymentStatus.Pending,
                Provider = entity.Provider ?? PaymentProvider.Mock,
                ProviderOrderCode = entity.ProviderOrderCode ?? entity.TransactionNo,
                QrContent = entity.QrContent ?? string.Empty,
                CheckoutUrl = entity.CheckoutUrl ?? string.Empty,
                CreatedAt = entity.CreatedAt ?? entity.PaymentDate,
                ExpiredAt = entity.ExpiredAt,
                PaidAt = entity.PaidAt
            };
        }
    }
}

