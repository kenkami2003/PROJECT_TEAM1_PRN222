
using System;
using System.ComponentModel.DataAnnotations;

namespace BoardingHouseManagement.Models
{
    public class Guardian
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid TenantId { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        [Phone]
        public string Phone { get; set; }

        [MaxLength(50)]
        public string Relationship { get; set; }
    }
}
