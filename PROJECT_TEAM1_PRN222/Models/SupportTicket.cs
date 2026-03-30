using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoardingHouseManagement.Models
{
    public enum TicketStatus
    {
        Pending,
        InProgress,
        Resolved,
        Closed
    }

    public class SupportTicket
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid TenantId { get; set; }

        [ForeignKey("TenantId")]
        public User? Tenant { get; set; }

        [Required, MaxLength(150)]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [MaxLength(255)]
        public string? AttachmentUrl { get; set; } // Hình ảnh/File đính kèm (nếu có)

        public TicketStatus Status { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        [MaxLength(500)]
        public string? AdminReply { get; set; } // Phản hồi từ Staff hoặc Admin
    }
}
