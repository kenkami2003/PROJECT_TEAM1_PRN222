
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
        public string? Condition { get; set; }

        public Room Room { get; set; }
        public AssetCategory? AssetCategory { get; set; }
    }
}
