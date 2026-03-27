using System;
using System.ComponentModel.DataAnnotations;

namespace BoardingHouseManagement.Models
{
    public class PaymentReceiverConfig
    {
        [Key]
        public Guid Id { get; set; }

        [Required, MaxLength(50)]
        public string BankCode { get; set; } = string.Empty;

        [Required, MaxLength(30)]
        public string AccountNumber { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string AccountName { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? BranchName { get; set; }

        [MaxLength(500)]
        public string? QrImageRelativePath { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime UpdatedAt { get; set; }
    }
}
