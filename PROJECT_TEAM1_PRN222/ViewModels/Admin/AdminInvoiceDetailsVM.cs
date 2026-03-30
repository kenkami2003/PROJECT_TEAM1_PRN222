
using BoardingHouseManagement.Models;
using System;
using System.Collections.Generic;

namespace BoardingHouseManagement.ViewModels.Admin
{
    public class AdminInvoiceDetailsVM
    {
        public Invoice? Invoice { get; set; }
        
        // Helper properties for easy display
        public string? TenantName => Invoice?.Contract?.Tenan?.FullName;
        public string? RoomNumber => Invoice?.Contract?.Room?.RoomNumber;
        public string? PropertyName => Invoice?.Contract?.Room?.Building?.Name;
        
        public IEnumerable<Payment> Payments => Invoice?.Payments ?? new List<Payment>();
    }
}
