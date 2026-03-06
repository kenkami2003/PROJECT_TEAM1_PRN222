
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

        public DateTime PaymentDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [MaxLength(50)]
        public string PaymentMethod { get; set; }

        [MaxLength(100)]
        public string TransactionNo { get; set; }

        public Invoice Invoice { get; set; }
    }
}
