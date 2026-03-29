using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using BoardingHouseManagement.Models;
using BoardingHouseManagement.Services.VnPay;

namespace BoardingHouseManagement.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly AppDbContext _context;
        private readonly IVnPayService _vnPayService;

        public PaymentService(AppDbContext context, IVnPayService vnPayService)
        {
            _context = context;
            _vnPayService = vnPayService;
        }

        public async Task<Invoice> GetInvoiceByIdAsync(Guid invoiceId)
        {
            return await _context.Invoices
                .Include(i => i.Contract)
                .ThenInclude(c => c.Tenan)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);
        }

        public async Task<Payment> CreatePendingVnPayPaymentAsync(Guid invoiceId, string currentUser)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Contract)
                .ThenInclude(c => c.Tenan)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);
            
            // Không tạo payment mới nếu invoice đã paid 
            if (invoice == null || invoice.Status == InvoiceStatus.Paid)
            {
                return null; 
            }

            // [SECURITY PASS]: Phải đúng người (Tenant) của Hợp đồng đó mới được thanh toán
            if (invoice.Contract?.Tenan?.Username != currentUser)
            {
                return null; // Chặn kịch bản User A đổi ID trên URL để quẹt thẻ cho User B
            }

            var txnRef = _vnPayService.BuildTxnRef();
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoiceId,
                Amount = invoice.TotalAmount,
                PaymentMethod = "VNPAY",
                Status = PaymentStatus.Pending,
                TxnRef = txnRef,
                CreatedAt = DateTime.Now,
                VnpResponseCode = string.Empty,
                VnpTransactionNo = string.Empty,
                VnpTransactionStatus = string.Empty
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            return payment;
        }

        public async Task<Payment> GetLatestByInvoiceIdAsync(Guid invoiceId)
        {
            return await _context.Payments
                .Where(p => p.InvoiceId == invoiceId)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<(bool IsSuccess, Guid? InvoiceId)> HandleVnPayReturnAsync(IQueryCollection queryParams)
        {
            // Bước 1: Verify checksum an toàn (IVnPayService)
            if (!_vnPayService.ValidateSignature(queryParams))
            {
                return (false, null); 
            }

            var (vnp_ResponseCode, vnp_TransactionNo, vnp_TxnRef) = _vnPayService.ParseReturnResponse(queryParams);
            
            // Lấy lượng tiền VNPAY trả ra (lưu ý chia 100)
            string vnp_Amount_Str = queryParams["vnp_Amount"].ToString();
            decimal vnpAmount = 0;
            if (decimal.TryParse(vnp_Amount_Str, out var amt))
            {
                vnpAmount = amt / 100;
            }

            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.TxnRef == vnp_TxnRef);
            if (payment == null) return (false, null);

            // Bước 2: Kiểm tra Amount khớp với TxnRef
            if (payment.Amount != vnpAmount)
            {
                return (false, payment.InvoiceId); // Security check: Amount mismatch! (Bị client fake url)
            }

            // Bước 3: Phân luồng success/failed
            if (vnp_ResponseCode == "00")
            {
                var success = await MarkSuccessAsync(vnp_TxnRef, vnp_TransactionNo);
                return (success, payment.InvoiceId);
            }
            else
            {
                var failed = await MarkFailedAsync(vnp_TxnRef, vnp_ResponseCode);
                return (false, payment.InvoiceId);
            }
        }

        public async Task<bool> MarkSuccessAsync(string txnRef, string vnpTransactionNo)
        {
            var payment = await _context.Payments
                .Include(p => p.Invoice)
                .FirstOrDefaultAsync(p => p.TxnRef == txnRef);

            // Xử lý an toàn nếu return bị gọi lặp (Idempotency)
            if (payment == null || payment.Status == PaymentStatus.Success)
            {
                return true; 
            }

            payment.Status = PaymentStatus.Success;
            payment.PaidAt = DateTime.Now;
            payment.VnpTransactionNo = vnpTransactionNo;
            payment.VnpResponseCode = "00";

            // Nếu thành công thì update Invoice = Paid
            if (payment.Invoice != null && payment.Invoice.Status != InvoiceStatus.Paid)
            {
                payment.Invoice.Status = InvoiceStatus.Paid;
                _context.Invoices.Update(payment.Invoice);
            }

            _context.Payments.Update(payment);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkFailedAsync(string txnRef, string vnpResponseCode)
        {
            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.TxnRef == txnRef);

            if (payment == null || payment.Status != PaymentStatus.Pending)
            {
                return false;
            }

            payment.Status = PaymentStatus.Failed;
            payment.VnpResponseCode = vnpResponseCode;

            _context.Payments.Update(payment);
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<IEnumerable<Payment>> GetAllPaymentsAsync(PaymentStatus? filterStatus)
        {
            var query = _context.Payments
                .Include(p => p.Invoice)
                .AsQueryable();

            if (filterStatus.HasValue)
            {
                query = query.Where(p => p.Status == filterStatus.Value);
            }

            return await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
        }
    }
}
