
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoardingHouseManagement.Models
{
    public class Room
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid BuildingId { get; set; }

        [Required, MaxLength(20)]
        public string RoomNumber { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BasePrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DepositAmount { get; set; }

        [Required]
        public RoomStatus Status { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        public Building Building { get; set; }
        public ICollection<RoomAsset> RoomAssets { get; set; }
        public ICollection<Contract> Contracts { get; set; }
    }
}
