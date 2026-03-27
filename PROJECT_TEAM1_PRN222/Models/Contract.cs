
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoardingHouseManagement.Models
{
    public class Contract
    {
        [Key]
        public Guid Id { get; set; }

        [Required, MaxLength(50)]
        public string ContractCode { get; set; }

        [Required]
        public Guid TenantId { get; set; }

        [ForeignKey("TenantId")]
        public virtual User? Tenan { get; set; }

        [Required]
        public Guid RoomId { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ActualDeposit { get; set; }

        public bool IsActive { get; set; }

        public Room Room { get; set; }
        public ICollection<Invoice> Invoices { get; set; }

        // thêm
        public string? ContractProofImage { get; set; } // Để lưu tên ảnh minh chứng chuyển khoản
        public DateTime CreatedAt { get; set; } = DateTime.Now; // Để biết hợp đồng tạo lúc nào
    }
}
