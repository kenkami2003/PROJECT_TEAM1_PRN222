
using System;
using System.ComponentModel.DataAnnotations;

namespace BoardingHouseManagement.Models
{
    public class AuditLog
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required, MaxLength(200)]
        public string Action { get; set; }

        public string Details { get; set; }

        public DateTime Timestamp { get; set; }
    }
}
