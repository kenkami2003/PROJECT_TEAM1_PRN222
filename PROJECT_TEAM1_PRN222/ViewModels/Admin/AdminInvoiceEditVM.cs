
using BoardingHouseManagement.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace BoardingHouseManagement.ViewModels.Admin
{
    public class AdminInvoiceEditVM
    {
        public Guid Id { get; set; }

        public string? InvoiceCode { get; set; }
        public string? TenantName { get; set; }
        public string? RoomNumber { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn tháng")]
        [Range(1, 12, ErrorMessage = "Tháng từ 1 đến 12")]
        public int Month { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn năm")]
        public int Year { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiền phòng")]
        [Range(0, double.MaxValue, ErrorMessage = "Số tiền không được âm")]
        public decimal RoomAmount { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Số tiền không được âm")]
        public decimal UtilityAmount { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Số tiền không được âm")]
        public decimal ServiceAmount { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Số tiền không được âm")]
        public decimal PenaltyAmount { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn hạn thanh toán")]
        public DateTime DueDate { get; set; }

        public InvoiceStatus Status { get; set; }
    }
}
