
using System;
using System.ComponentModel.DataAnnotations;

namespace BoardingHouseManagement.Models
{
    public class ContractTerm
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid ContractId { get; set; }

        [Required]
        public string Content { get; set; }
    }
}
