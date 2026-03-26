using System;

namespace BoardingHouseManagement.Services.Invoice
{
    public class ContractSelectItemDto
    {
        public Guid Id { get; init; }
        public string Label { get; init; } = string.Empty;
    }
}
