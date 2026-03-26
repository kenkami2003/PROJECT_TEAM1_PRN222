
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BoardingHouseManagement.Models
{
    public class Property
    {
        [Key]
        public Guid Id { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; }

        [Required, MaxLength(300)]
        public string Address { get; set; }

        public ICollection<Building> Buildings { get; set; }
    }
}
