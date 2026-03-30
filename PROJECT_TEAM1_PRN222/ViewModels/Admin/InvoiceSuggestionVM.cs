using System;

namespace BoardingHouseManagement.ViewModels.Admin
{
    public class InvoiceSuggestionVM
    {
        // 1. Phí cơ bản
        public decimal RoomAmount { get; set; }
        public decimal ServiceAmount { get; set; }
        public decimal UtilityAmount { get; set; }
        public decimal TotalAmount { get; set; } // RoomAmount + ServiceAmount + UtilityAmount

        // 2. Chi tiết tiện ích (Điện / Nước)
        public decimal ElectricityAmount { get; set; }
        public decimal WaterAmount { get; set; }

        public double OldElectricity { get; set; }
        public double NewElectricity { get; set; }
        public double OldWater { get; set; }
        public double NewWater { get; set; }

        // 3. Trạng thái và Cảnh báo
        public bool HasUtilityReading { get; set; } // Trạng thái đã chốt điện/nước tháng này chưa
        public bool IsDuplicateInvoice { get; set; } // Trạng thái hợp đồng tháng này đã có hóa đơn chưa
        public string? WarningMessage { get; set; }
    }
}
