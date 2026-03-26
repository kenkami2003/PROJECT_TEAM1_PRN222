using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BoardingHouseManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace BoardingHouseManagement.Services.Payment
{
    public class PaymentService : IPaymentService
    {
        private readonly AppDbContext _context;

        // 15 minutes pending TTL as requested.
        private static readonly TimeSpan PendingTtl = TimeSpan.FromMinutes(15);

        public PaymentService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<CreateOrGetPaymentResultDto> CreateOrGetPaymentAsync(Guid invoiceId)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Contract)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);
            if (invoice == null) throw new KeyNotFoundException($"Invoice not found: {invoiceId}");

            var tenantName = invoice.Contract != null
                ? await _context.Users
                    .Where(u => u.Id == invoice.Contract.TenantId)
                    .Select(u => u.FullName)
                    .FirstOrDefaultAsync()
                : null;

            tenantName ??= string.Empty;

            // 2) If invoice already paid -> do not create new attempt.
            if (invoice.Status == InvoiceStatus.Paid)
            {
                var latestPaidPayment = await _context.Payments
                    .Where(p => p.InvoiceId == invoiceId && (p.Status == PaymentStatus.Success || p.PaidAt != null))
                    .OrderByDescending(p => p.PaidAt ?? p.ConfirmedAt ?? p.CreatedAt ?? p.PaymentDate)
                    .FirstOrDefaultAsync();

                return CreateOrGetPaymentResultDto.FromInvoice(
                    invoice,
                    invoicePaid: true,
                    tenantName,
                    latestPaidPayment == null ? null : PaymentDto.FromEntity(latestPaidPayment)
                );
            }

            var now = DateTime.UtcNow;

            // 3) If customer already marked paid, reuse waiting-confirmation attempt
            var waitingPayment = await _context.Payments
                .Where(p => p.InvoiceId == invoiceId && p.Status == PaymentStatus.WaitingConfirmation)
                .OrderByDescending(p => p.CustomerMarkedPaidAt ?? p.CreatedAt ?? p.PaymentDate)
                .FirstOrDefaultAsync();

            if (waitingPayment != null)
            {
                return CreateOrGetPaymentResultDto.FromInvoice(
                    invoice,
                    invoicePaid: false,
                    tenantName,
                    PaymentDto.FromEntity(waitingPayment)
                );
            }

            // 4) Find pending & not expired payment attempt
            var pendingPayment = await _context.Payments
                .Where(p =>
                    p.InvoiceId == invoiceId &&
                    p.Status == PaymentStatus.Pending &&
                    p.ExpiredAt != null &&
                    p.ExpiredAt > now)
                .OrderByDescending(p => p.CreatedAt ?? p.PaymentDate)
                .FirstOrDefaultAsync();

            // 5) If exists -> return it
            if (pendingPayment != null)
            {
                if (pendingPayment.Amount != invoice.TotalAmount)
                {
                    pendingPayment.Amount = invoice.TotalAmount;
                    ApplyQrDetails(
                        pendingPayment,
                        await GetActiveReceiverConfigAsync(),
                        invoice.InvoiceCode,
                        pendingPayment.ProviderOrderCode ?? pendingPayment.TransactionNo);
                    await _context.SaveChangesAsync();
                }

                return CreateOrGetPaymentResultDto.FromInvoice(
                    invoice,
                    invoicePaid: false,
                    tenantName,
                    PaymentDto.FromEntity(pendingPayment)
                );
            }

            // 6) Otherwise create a new payment attempt
            var providerOrderCode = GenerateUniqueProviderOrderCode();
            var createdAt = now;
            var expiredAt = now.Add(PendingTtl);
            var config = await GetActiveReceiverConfigAsync();

            var payment = new BoardingHouseManagement.Models.Payment
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoiceId,
                Amount = invoice.TotalAmount,
                // Legacy fields (required by existing DB schema)
                PaymentDate = now,
                PaymentMethod = PaymentProvider.Mock.ToString(),
                TransactionNo = providerOrderCode,

                // New fields for QR static + manual confirmation flow
                Status = PaymentStatus.Pending,
                Provider = PaymentProvider.Mock,
                ProviderOrderCode = providerOrderCode,
                QrContent = string.Empty,
                CheckoutUrl = string.Empty,
                CreatedAt = createdAt,
                ExpiredAt = expiredAt,
                PaidAt = null,
                CustomerMarkedPaidAt = null,
                ConfirmedAt = null,
                RejectedAt = null,
                RejectionNote = null
            };
            ApplyQrDetails(payment, config, invoice.InvoiceCode, providerOrderCode);

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            return CreateOrGetPaymentResultDto.FromInvoice(
                invoice,
                invoicePaid: false,
                tenantName,
                PaymentDto.FromEntity(payment)
            );
        }

        public async Task<PaymentDto> CustomerMarkPaidAsync(Guid paymentId)
        {
            var payment = await _context.Payments
                .Include(p => p.Invoice)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null) throw new KeyNotFoundException($"Payment not found: {paymentId}");

            // If invoice is already paid, don't let customer mark again.
            if (payment.Invoice.Status == InvoiceStatus.Paid)
            {
                return PaymentDto.FromEntity(payment);
            }

            if (payment.Status != PaymentStatus.Pending)
            {
                throw new InvalidOperationException("Payment is not in Pending state.");
            }

            if (payment.ExpiredAt == null || payment.ExpiredAt <= DateTime.UtcNow)
            {
                throw new InvalidOperationException("Payment attempt has expired.");
            }

            payment.Status = PaymentStatus.WaitingConfirmation;
            payment.CustomerMarkedPaidAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return PaymentDto.FromEntity(payment);
        }

        public async Task<PaymentDto> AdminConfirmAsync(Guid paymentId)
        {
            var payment = await _context.Payments
                .Include(p => p.Invoice)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null) throw new KeyNotFoundException($"Payment not found: {paymentId}");

            if (payment.Invoice.Status == InvoiceStatus.Paid)
            {
                return PaymentDto.FromEntity(payment);
            }

            if (payment.Status != PaymentStatus.WaitingConfirmation)
            {
                throw new InvalidOperationException("Payment is not waiting for confirmation.");
            }

            var now = DateTime.UtcNow;

            payment.Status = PaymentStatus.Success;
            payment.ConfirmedAt = now;
            payment.PaidAt = now;

            // Confirming payment means invoice becomes Paid.
            payment.Invoice.Status = InvoiceStatus.Paid;

            await _context.SaveChangesAsync();

            return PaymentDto.FromEntity(payment);
        }

        public async Task<PaymentDto> AdminRejectAsync(Guid paymentId, string? note)
        {
            var payment = await _context.Payments
                .Include(p => p.Invoice)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null) throw new KeyNotFoundException($"Payment not found: {paymentId}");

            if (payment.Invoice.Status == InvoiceStatus.Paid)
            {
                return PaymentDto.FromEntity(payment);
            }

            if (payment.Status != PaymentStatus.WaitingConfirmation)
            {
                throw new InvalidOperationException("Payment is not waiting for confirmation.");
            }

            payment.Status = PaymentStatus.Rejected;
            payment.RejectedAt = DateTime.UtcNow;
            payment.RejectionNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();

            await _context.SaveChangesAsync();

            return PaymentDto.FromEntity(payment);
        }

        public async Task<List<WaitingConfirmationPaymentDto>> GetWaitingConfirmationsAsync()
        {
            // Join Users to display tenant name.
            var query =
                from p in _context.Payments
                join i in _context.Invoices on p.InvoiceId equals i.Id
                join c in _context.Contracts on i.ContractId equals c.Id
                join u in _context.Users on c.TenantId equals u.Id
                where p.Status == PaymentStatus.WaitingConfirmation
                select new WaitingConfirmationPaymentDto
                {
                    PaymentId = p.Id,
                    InvoiceId = i.Id,
                    InvoiceCode = i.InvoiceCode,
                    TenantName = u.FullName,
                    Amount = p.Amount,
                    ProviderOrderCode = p.ProviderOrderCode ?? p.TransactionNo,
                    QrContent = p.QrContent ?? string.Empty,
                    CustomerMarkedPaidAt = p.CustomerMarkedPaidAt ?? DateTime.MinValue
                };

            var list = await query
                .OrderByDescending(x => x.CustomerMarkedPaidAt)
                .ToListAsync();

            // Safety: if any legacy row has null CustomerMarkedPaidAt, don't show it as pending.
            return list.Where(x => x.CustomerMarkedPaidAt != DateTime.MinValue).ToList();
        }

        public async Task<List<TenantInvoicePaymentItemDto>> GetTenantInvoicesAsync(Guid tenantId)
        {
            var invoices = await (
                from i in _context.Invoices
                join c in _context.Contracts on i.ContractId equals c.Id
                where c.TenantId == tenantId
                select new TenantInvoicePaymentItemDto
                {
                    InvoiceId = i.Id,
                    InvoiceCode = i.InvoiceCode,
                    TotalAmount = i.TotalAmount,
                    InvoiceStatus = i.Status,
                    Month = i.Month,
                    Year = i.Year
                })
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .ToListAsync();

            return invoices;
        }

        private static string GenerateUniqueProviderOrderCode()
        {
            return $"MOCK-{Guid.NewGuid():N}";
        }

        private async Task<PaymentReceiverConfig?> GetActiveReceiverConfigAsync()
        {
            return await _context.PaymentReceiverConfigs
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.UpdatedAt)
                .FirstOrDefaultAsync();
        }

        private static void ApplyQrDetails(
            BoardingHouseManagement.Models.Payment payment,
            PaymentReceiverConfig? config,
            string invoiceCode,
            string providerOrderCode)
        {
            var codeTail = providerOrderCode.Length > 8 ? providerOrderCode[..8] : providerOrderCode;
            var transferContent = $"TT {invoiceCode} {codeTail}";
            payment.CheckoutUrl = transferContent;

            if (config == null || string.IsNullOrWhiteSpace(config.BankCode) || string.IsNullOrWhiteSpace(config.AccountNumber))
            {
                payment.QrContent = string.Empty;
                return;
            }

            var amount = Convert.ToInt64(Math.Round(payment.Amount, MidpointRounding.AwayFromZero));
            var encodedInfo = WebUtility.UrlEncode(transferContent);
            var encodedName = WebUtility.UrlEncode(config.AccountName ?? string.Empty);
            payment.QrContent =
                $"https://img.vietqr.io/image/{config.BankCode}-{config.AccountNumber}-compact2.png?amount={amount}&addInfo={encodedInfo}&accountName={encodedName}";
        }
    }
}

