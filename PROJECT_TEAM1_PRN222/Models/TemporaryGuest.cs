
using System;
using System.ComponentModel.DataAnnotations;

namespace BoardingHouseManagement.Models
{
    public class TemporaryGuest
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid RoomId { get; set; }

        // Thêm dòng này để dễ dàng lấy tên phòng/giá phòng trong Code
        public virtual Room Room { get; set; }

        [Required, MaxLength(100)]
        public string FullName { get; set; }

        [MaxLength(20)]
        public string IdentityNumber { get; set; }

        public DateTime ArrivalDate { get; set; }
        public DateTime DepartureDate { get; set; }

        // Nên lưu lại số tiền thực tế khách đã trả lúc đăng ký
        public decimal TotalAmount { get; set; }

        [MaxLength(500)]
        public string Note { get; set; }

        public bool IsCheckedOut { get; set; } // Quản lý xem khách đã đi chư
        public string PhoneNumber { get; set; }        // Số điện thoại khách
        public string? IdentityCardFront { get; set; } // Ảnh mặt trước CCCD
        public string? IdentityCardBack { get; set; }  // Ảnh mặt sau CCCD
        public string? PaymentProof { get; set; }      // Ảnh biên lai do Admin chụp/up
        public bool IsPaid { get; set; }              // Trạng thái thanh toán
    }
}
