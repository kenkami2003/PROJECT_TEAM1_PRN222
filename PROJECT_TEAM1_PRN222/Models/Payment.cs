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

        [MaxLength(50)]
        public string PaymentMethod { get; set; }

        public PaymentStatus Status { get; set; } // Enum mới thêm

        [MaxLength(100)]
        public string TxnRef { get; set; } // Mã tham chiếu (mã đơn hàng cục bộ) gửi lên VNPAY

        [MaxLength(100)]
        public string VnpTransactionNo { get; set; } // Mã giao dịch ghi nhận tại hệ thống VNPAY

        [MaxLength(20)]
        public string VnpResponseCode { get; set; } // Mã phản hồi từ VNPAY

        [MaxLength(20)]
        public string VnpTransactionStatus { get; set; } // Trạng thái giao dịch từ VNPAY

        public DateTime CreatedAt { get; set; }

        public DateTime? PaidAt { get; set; } // Nullable, tuỳ ý khi thanh toán xong mới gán

        public Invoice Invoice { get; set; }
    }
}
