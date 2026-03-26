
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoardingHouseManagement.Models
{
    public class Payment
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid InvoiceId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        // Legacy columns (already exist in DB from the original schema).
        // Keeping them prevents EF migrations from dropping existing columns.
        public DateTime PaymentDate { get; set; }

        [Required, MaxLength(50)]
        public string PaymentMethod { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string TransactionNo { get; set; } = string.Empty;

        // New fields for QR static + manual confirmation flow.
        public PaymentStatus? Status { get; set; }

        public PaymentProvider? Provider { get; set; }

        [MaxLength(100)]
        public string? ProviderOrderCode { get; set; }

        [MaxLength(2000)]
        public string? QrContent { get; set; }

        [MaxLength(2048)]
        public string? CheckoutUrl { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? ExpiredAt { get; set; }

        public DateTime? CustomerMarkedPaidAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public DateTime? RejectedAt { get; set; }

        [MaxLength(2000)]
        public string? RejectionNote { get; set; }

        // Used when landlord confirms successfully (also the point invoice becomes Paid).
        public DateTime? PaidAt { get; set; }

        public Invoice Invoice { get; set; }
    }
}
