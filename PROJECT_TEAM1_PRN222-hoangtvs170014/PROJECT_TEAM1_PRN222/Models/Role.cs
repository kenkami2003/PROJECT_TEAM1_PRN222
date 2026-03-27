
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BoardingHouseManagement.Models
{
    public class Role
    {
        [Key]
        public Guid Id { get; set; }

        [Required, MaxLength(50)]
        public string RoleName { get; set; }

        public ICollection<User> Users { get; set; }
    }
}
