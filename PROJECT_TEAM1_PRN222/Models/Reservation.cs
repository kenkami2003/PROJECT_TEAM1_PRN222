using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoardingHouseManagement.Models
{
    public enum ReservationStatus
    {
        Pending = 0,    // Đang chờ thanh toán/upload ảnh
        Confirmed = 1,  // Admin đã xác nhận tiền
        Cancelled = 2   // Đã hủy (do quá hạn hoặc khách hủy)
    }

    public class Reservation
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid RoomId { get; set; }

        [Required]
        public Guid GuestId { get; set; }

        [ForeignKey("GuestId")]
        public virtual User? Guest { get; set; }

        public DateTime ReservedDate { get; set; } // Ngày khách chọn giữ chỗ

        // THÊM DÒNG NÀY: Để tính toán 24h tự động hủy
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime ExpiryDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ReservationFee { get; set; }

        public bool IsConvertedToContract { get; set; }

        [ForeignKey("RoomId")]
        public virtual Room? Room { get; set; }

        public string? Note { get; set; }

        public string? PaymentProofImage { get; set; }

        // Mặc định là Pending khi mới tạo
        public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
    }
}