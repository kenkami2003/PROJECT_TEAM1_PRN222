
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoardingHouseManagement.Models
{
    public class Invoice
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid ContractId { get; set; }

        [Required, MaxLength(50)]
        public string InvoiceCode { get; set; }

        public int Month { get; set; }
        public int Year { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal RoomAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UtilityAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ServiceAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PenaltyAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public InvoiceStatus Status { get; set; }

        public Contract Contract { get; set; }
        public ICollection<Payment> Payments { get; set; }
    }
}
