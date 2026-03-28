namespace PROJECT_TEAM1_PRN222.ViewModel
{
    public class PaymentVerificationViewModel
    {
        public Guid Id { get; set; } // Id của Reservation hoặc Contract
        public string FullName { get; set; }
        public string ImagePath { get; set; }
        public string Type { get; set; } // "Giữ chỗ" hoặc "Hợp đồng"
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsConfirmed { get; set; }
        public string Status { get; set; } // Trạng thái hiển thị (Đã duyệt/Chờ)
    }
}
