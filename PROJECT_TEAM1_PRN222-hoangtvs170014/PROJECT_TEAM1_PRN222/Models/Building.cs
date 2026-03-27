
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BoardingHouseManagement.Models
{
    public class Building
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid PropertyId { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        public Property Property { get; set; }
        public ICollection<Room> Rooms { get; set; }
    }
}
