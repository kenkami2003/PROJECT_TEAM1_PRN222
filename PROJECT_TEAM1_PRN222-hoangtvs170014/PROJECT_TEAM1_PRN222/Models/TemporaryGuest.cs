
using System;
using System.ComponentModel.DataAnnotations;

namespace BoardingHouseManagement.Models
{
    public class TemporaryGuest
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid RoomId { get; set; }

        [Required, MaxLength(100)]
        public string FullName { get; set; }

        [MaxLength(20)]
        public string IdentityNumber { get; set; }

        public DateTime ArrivalDate { get; set; }
        public DateTime DepartureDate { get; set; }

        [MaxLength(500)]
        public string Note { get; set; }
    }
}
