
using System;
using System.ComponentModel.DataAnnotations;

namespace BoardingHouseManagement.Models
{
    public class MaintenanceRequest
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid RoomId { get; set; }

        [Required]
        public Guid TenantId { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; }

        public string Description { get; set; }

        public string ImageUrl { get; set; }

        public RequestStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }

        public Room Room { get; set; }
    }
}
