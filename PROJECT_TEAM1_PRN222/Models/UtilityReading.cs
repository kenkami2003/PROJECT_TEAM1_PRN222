
using System;
using System.ComponentModel.DataAnnotations;

namespace BoardingHouseManagement.Models
{
    public class UtilityReading
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid RoomId { get; set; }

        [Range(1,12)]
        public int Month { get; set; }

        public int Year { get; set; }

        public double OldElectricity { get; set; }
        public double NewElectricity { get; set; }
 

        public double OldWater { get; set; }
        public double NewWater { get; set; }

        public DateTime ReadingDate { get; set; }

        public Guid RecordedBy { get; set; }

        public Room? Room { get; set; }
    }
}
