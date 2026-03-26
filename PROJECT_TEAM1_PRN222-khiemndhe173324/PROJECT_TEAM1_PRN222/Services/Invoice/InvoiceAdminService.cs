using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BoardingHouseManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace BoardingHouseManagement.Services.Invoice
{
    public class InvoiceAdminService : IInvoiceAdminService
    {
        private readonly AppDbContext _context;

        public InvoiceAdminService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<InvoiceListItemDto>> GetInvoicesAsync()
        {
            var list = await (
                from i in _context.Invoices
                join c in _context.Contracts on i.ContractId equals c.Id
                join u in _context.Users on c.TenantId equals u.Id
                join r in _context.Rooms on c.RoomId equals r.Id
                orderby i.Year descending, i.Month descending
                select new InvoiceListItemDto
                {
                    Id = i.Id,
                    InvoiceCode = i.InvoiceCode,
                    Month = i.Month,
                    Year = i.Year,
                    TenantName = u.FullName,
                    ContractCode = c.ContractCode,
                    RoomNumber = r.RoomNumber,
                    TotalAmount = i.TotalAmount,
                    Status = i.Status
                })
                .ToListAsync();

            if (list.Count == 0)
                return list;

            var invoiceIds = list.Select(x => x.Id).ToList();
            var pendingRows = await _context.Payments
                .AsNoTracking()
                .Where(p => invoiceIds.Contains(p.InvoiceId) && p.Status == PaymentStatus.WaitingConfirmation)
                .Select(p => new { p.InvoiceId, p.Id, p.CustomerMarkedPaidAt, p.CreatedAt })
                .ToListAsync();

            var pendingByInvoice = pendingRows
                .GroupBy(x => x.InvoiceId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(x => x.CustomerMarkedPaidAt ?? x.CreatedAt).First().Id);

            foreach (var item in list)
            {
                if (pendingByInvoice.TryGetValue(item.Id, out var paymentId))
                    item.WaitingConfirmationPaymentId = paymentId;
            }

            return list;
        }

        public async Task<List<ContractSelectItemDto>> GetActiveContractsForSelectAsync()
        {
            return await (
                from c in _context.Contracts
                where c.IsActive
                join u in _context.Users on c.TenantId equals u.Id
                join r in _context.Rooms on c.RoomId equals r.Id
                orderby c.ContractCode
                select new ContractSelectItemDto
                {
                    Id = c.Id,
                    Label = $"{c.ContractCode} — {u.FullName} — Phòng {r.RoomNumber}"
                })
                .ToListAsync();
        }

        public async Task<InvoiceEditDto?> GetForEditAsync(Guid id)
        {
            var inv = await _context.Invoices.FirstOrDefaultAsync(i => i.Id == id);
            if (inv == null) return null;

            if (inv.Status == InvoiceStatus.Paid)
                throw new InvalidOperationException("Không sửa hóa đơn đã thanh toán.");

            if (await HasWaitingConfirmationPaymentAsync(inv.Id))
                throw new InvalidOperationException("Hóa đơn đang chờ xác nhận thanh toán, không thể sửa số tiền.");

            var contracts = await GetActiveContractsForSelectAsync();
            return new InvoiceEditDto
            {
                Id = inv.Id,
                ContractId = inv.ContractId,
                InvoiceCode = inv.InvoiceCode,
                Month = inv.Month,
                Year = inv.Year,
                RoomAmount = inv.RoomAmount,
                UtilityAmount = inv.UtilityAmount,
                ServiceAmount = inv.ServiceAmount,
                PenaltyAmount = inv.PenaltyAmount,
                Contracts = contracts
            };
        }

        public async Task<InvoiceEditDto> GetEmptyForCreateAsync()
        {
            var now = DateTime.Now;
            var contracts = await GetActiveContractsForSelectAsync();
            return new InvoiceEditDto
            {
                Id = null,
                ContractId = contracts.FirstOrDefault()?.Id ?? Guid.Empty,
                InvoiceCode = string.Empty,
                Month = now.Month,
                Year = now.Year,
                RoomAmount = 0,
                UtilityAmount = 0,
                ServiceAmount = 0,
                PenaltyAmount = 0,
                Contracts = contracts
            };
        }

        public async Task<Guid> CreateAsync(InvoiceEditDto input)
        {
            if (input.ContractId == Guid.Empty)
                throw new InvalidOperationException("Chọn hợp đồng.");

            if (input.Month is < 1 or > 12)
                throw new InvalidOperationException("Tháng không hợp lệ.");

            if (input.Year < 2000 || input.Year > 2100)
                throw new InvalidOperationException("Năm không hợp lệ.");

            var exists = await _context.Invoices.AnyAsync(i =>
                i.ContractId == input.ContractId && i.Month == input.Month && i.Year == input.Year);
            if (exists)
                throw new InvalidOperationException("Đã có hóa đơn cho hợp đồng và kỳ tháng/năm này.");

            var contract = await _context.Contracts.FirstOrDefaultAsync(c => c.Id == input.ContractId);
            if (contract == null || !contract.IsActive)
                throw new InvalidOperationException("Hợp đồng không hợp lệ.");

            var total = ComputeTotal(input);
            var code = await GenerateUniqueInvoiceCodeAsync(input.Year, input.Month);

            var entity = new BoardingHouseManagement.Models.Invoice
            {
                Id = Guid.NewGuid(),
                ContractId = input.ContractId,
                InvoiceCode = code,
                Month = input.Month,
                Year = input.Year,
                RoomAmount = input.RoomAmount,
                UtilityAmount = input.UtilityAmount,
                ServiceAmount = input.ServiceAmount,
                PenaltyAmount = input.PenaltyAmount,
                TotalAmount = total,
                Status = InvoiceStatus.Pending
            };

            _context.Invoices.Add(entity);
            await _context.SaveChangesAsync();

            await RefreshPendingPaymentsForInvoiceAsync(entity.Id, regenerateQr: false);
            return entity.Id;
        }

        public async Task UpdateAsync(InvoiceEditDto input)
        {
            if (input.Id == null || input.Id == Guid.Empty)
                throw new InvalidOperationException("Thiếu mã hóa đơn.");

            var inv = await _context.Invoices.FirstOrDefaultAsync(i => i.Id == input.Id);
            if (inv == null)
                throw new KeyNotFoundException();

            if (inv.Status == InvoiceStatus.Paid)
                throw new InvalidOperationException("Không sửa hóa đơn đã thanh toán.");

            if (await HasWaitingConfirmationPaymentAsync(inv.Id))
                throw new InvalidOperationException("Hóa đơn đang chờ xác nhận thanh toán, không thể sửa số tiền.");

            if (input.Month is < 1 or > 12)
                throw new InvalidOperationException("Tháng không hợp lệ.");

            if (input.Year < 2000 || input.Year > 2100)
                throw new InvalidOperationException("Năm không hợp lệ.");

            var duplicate = await _context.Invoices.AnyAsync(i =>
                i.ContractId == input.ContractId &&
                i.Month == input.Month &&
                i.Year == input.Year &&
                i.Id != inv.Id);
            if (duplicate)
                throw new InvalidOperationException("Đã có hóa đơn khác cho hợp đồng và kỳ tháng/năm này.");

            var contract = await _context.Contracts.FirstOrDefaultAsync(c => c.Id == input.ContractId);
            if (contract == null || !contract.IsActive)
                throw new InvalidOperationException("Hợp đồng không hợp lệ.");

            var oldTotal = inv.TotalAmount;
            inv.ContractId = input.ContractId;
            inv.Month = input.Month;
            inv.Year = input.Year;
            inv.RoomAmount = input.RoomAmount;
            inv.UtilityAmount = input.UtilityAmount;
            inv.ServiceAmount = input.ServiceAmount;
            inv.PenaltyAmount = input.PenaltyAmount;
            inv.TotalAmount = ComputeTotal(input);

            await _context.SaveChangesAsync();
            var amountChanged = oldTotal != inv.TotalAmount;
            await RefreshPendingPaymentsForInvoiceAsync(inv.Id, regenerateQr: amountChanged);
        }

        private static decimal ComputeTotal(InvoiceEditDto input)
        {
            return input.RoomAmount + input.UtilityAmount + input.ServiceAmount + input.PenaltyAmount;
        }

        private async Task<string> GenerateUniqueInvoiceCodeAsync(int year, int month)
        {
            for (var i = 0; i < 20; i++)
            {
                var code = $"INV-{year}{month:D2}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
                var taken = await _context.Invoices.AnyAsync(x => x.InvoiceCode == code);
                if (!taken) return code;
            }

            return $"INV-{year}{month:D2}-{Guid.NewGuid():N}";
        }

        private async Task<bool> HasWaitingConfirmationPaymentAsync(Guid invoiceId)
        {
            return await _context.Payments.AnyAsync(p =>
                p.InvoiceId == invoiceId &&
                p.Status == PaymentStatus.WaitingConfirmation);
        }

        private async Task RefreshPendingPaymentsForInvoiceAsync(Guid invoiceId, bool regenerateQr)
        {
            var invoice = await _context.Invoices.AsNoTracking().FirstOrDefaultAsync(i => i.Id == invoiceId);
            if (invoice == null) return;

            var now = DateTime.UtcNow;
            var config = await _context.PaymentReceiverConfigs
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.UpdatedAt)
                .FirstOrDefaultAsync();
            var pendings = await _context.Payments
                .Where(p =>
                    p.InvoiceId == invoiceId &&
                    p.Status == PaymentStatus.Pending &&
                    p.ExpiredAt != null &&
                    p.ExpiredAt > now)
                .ToListAsync();

            foreach (var p in pendings)
            {
                p.Amount = invoice.TotalAmount;
                if (regenerateQr)
                {
                    var providerOrderCode = GenerateUniqueProviderOrderCode();
                    p.ProviderOrderCode = providerOrderCode;
                    p.TransactionNo = providerOrderCode;
                    var transferContent = BuildTransferContent(invoice.InvoiceCode, providerOrderCode);
                    p.CheckoutUrl = transferContent;
                    p.QrContent = BuildQrImageUrl(config, p.Amount, transferContent);
                    p.CreatedAt = now;
                    p.PaymentDate = now;
                    p.ExpiredAt = now.AddMinutes(15);
                }
            }

            if (pendings.Count > 0)
                await _context.SaveChangesAsync();
        }

        private static string GenerateUniqueProviderOrderCode()
        {
            return $"MOCK-{Guid.NewGuid():N}";
        }

        private static string BuildTransferContent(string invoiceCode, string providerOrderCode)
        {
            var codeTail = providerOrderCode.Length > 8 ? providerOrderCode[..8] : providerOrderCode;
            return $"TT {invoiceCode} {codeTail}";
        }

        private static string BuildQrImageUrl(PaymentReceiverConfig? config, decimal amount, string transferContent)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.BankCode) || string.IsNullOrWhiteSpace(config.AccountNumber))
                return string.Empty;

            var paymentAmount = Convert.ToInt64(Math.Round(amount, MidpointRounding.AwayFromZero));
            var encodedInfo = WebUtility.UrlEncode(transferContent);
            var encodedName = WebUtility.UrlEncode(config.AccountName ?? string.Empty);
            return $"https://img.vietqr.io/image/{config.BankCode}-{config.AccountNumber}-compact2.png?amount={paymentAmount}&addInfo={encodedInfo}&accountName={encodedName}";
        }
    }
}
