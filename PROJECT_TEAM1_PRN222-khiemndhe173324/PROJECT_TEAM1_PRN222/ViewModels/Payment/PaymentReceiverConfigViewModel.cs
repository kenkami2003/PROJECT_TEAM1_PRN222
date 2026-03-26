using System;
using System.ComponentModel.DataAnnotations;

namespace PROJECT_TEAM1_PRN222.ViewModels.Payment
{
    public class PaymentReceiverConfigViewModel
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "Nhap ma ngan hang")]
        [MaxLength(50)]
        public string BankCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nhap so tai khoan")]
        [MaxLength(30)]
        public string AccountNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nhap ten chu tai khoan")]
        [MaxLength(150)]
        public string AccountName { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? BranchName { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? UpdatedAt { get; set; }

        public string? QrImageRelativePath { get; set; }
    }
}
