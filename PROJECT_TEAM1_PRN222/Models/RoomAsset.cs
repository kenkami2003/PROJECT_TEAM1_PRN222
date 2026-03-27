
using System;
using System.ComponentModel.DataAnnotations;

namespace BoardingHouseManagement.Models
{
    public class RoomAsset
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid RoomId { get; set; }

        [Required]
        public Guid AssetCategoryId { get; set; }

        public int Quantity { get; set; }

        [MaxLength(100)]
        public string? Condition { get; set; } // Ví dụ: Mới, 90%, Hỏng nhẹ...

        // --- CÁC TRƯỜNG THÊM MỚI ---

        [MaxLength(500)]
        public string? ImageUrl { get; set; } // Lưu đường dẫn file ảnh: /uploads/assets/dieu-hoa.jpg

        public DateTime? InstalledDate { get; set; } // Ngày trang bị tài sản vào phòng

        [MaxLength(250)]
        public string? Note { get; set; } // Ghi chú chi tiết (ví dụ: Hiệu Samsung, 1.5HP)

        // Navigation Properties
        public Room? Room { get; set; }
        public AssetCategory? AssetCategory { get; set; }
    }
}
