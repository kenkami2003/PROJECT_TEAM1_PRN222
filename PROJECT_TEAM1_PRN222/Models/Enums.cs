
using System;

namespace BoardingHouseManagement.Models
{
    public enum RoomStatus { Available, Occupied, Reserved, Maintenance }
    public enum InvoiceStatus { Pending, Paid, Overdue }
    public enum RequestStatus { Open, Processing, Fixed }
    public enum PaymentStatus { Pending, Success, Failed, Cancelled }
}
