
using BoardingHouseManagement.Models;
using System.Collections.Generic;

namespace BoardingHouseManagement.ViewModels.Admin
{
    public class AdminInvoiceListVM
    {
        public IEnumerable<Invoice> Invoices { get; set; }
        public int? SelectedMonth { get; set; }
        public int? SelectedYear { get; set; }
        public InvoiceStatus? SelectedStatus { get; set; }
    }
}
