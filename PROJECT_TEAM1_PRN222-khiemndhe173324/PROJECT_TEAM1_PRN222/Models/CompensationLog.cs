
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoardingHouseManagement.Models
{
    public class CompensationLog
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid ContractId { get; set; }

        [Required]
        public Guid RoomAssetId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public string Reason { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
