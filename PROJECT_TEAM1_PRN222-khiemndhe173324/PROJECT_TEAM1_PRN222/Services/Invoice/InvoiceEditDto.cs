using System;
using System.Collections.Generic;

namespace BoardingHouseManagement.Services.Invoice
{
    public class InvoiceEditDto
    {
        public Guid? Id { get; set; }
        public Guid ContractId { get; set; }
        public string InvoiceCode { get; set; } = string.Empty;
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal RoomAmount { get; set; }
        public decimal UtilityAmount { get; set; }
        public decimal ServiceAmount { get; set; }
        public decimal PenaltyAmount { get; set; }
        public List<ContractSelectItemDto> Contracts { get; set; } = new();
    }
}
