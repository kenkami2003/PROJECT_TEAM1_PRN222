using System;
using System.Collections.Generic;

namespace BoardingHouseManagement.Models
{
    public class UtilityBatchViewModel
    {
        public Guid PropertyId { get; set; }
        public string PropertyName { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public List<UtilityEntryItem> Rooms { get; set; } = new List<UtilityEntryItem>();
    }

    public class UtilityEntryItem
    {
        public Guid RoomId { get; set; }
        public string RoomNumber { get; set; }
        public string BuildingName { get; set; }
        public decimal BasePrice { get; set; }
        public double ServiceFee { get; set; }
        public double PriceUnitElectricity { get; set; }
        public double PriceUnitWater { get; set; }

        // Số cũ (Lấy từ bản ghi tháng gần nhất trong DB)
        public double OldElectricity { get; set; }
        public double OldWater { get; set; }

        // Số mới (Người dùng nhập vào)
        public double NewElectricity { get; set; }
        public double NewWater { get; set; }
    }
}