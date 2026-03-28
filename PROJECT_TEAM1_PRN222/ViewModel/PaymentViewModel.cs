using System;
using BoardingHouseManagement.Models;

namespace BoardingHouseManagement.ViewModel
{
    public class PaymentViewModel
    {
        public Guid InvoiceId { get; set; }
        public string InvoiceCode { get; set; }
        public decimal TotalAmount { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        // Để hiển thị phụ cho user biết
        public string Username { get; set; }
        
        // Dùng để submit form
        public string PaymentMethod { get; set; }
    }
}
