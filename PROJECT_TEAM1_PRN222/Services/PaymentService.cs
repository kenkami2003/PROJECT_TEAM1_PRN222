using System;
using System.Linq;
using System.Threading.Tasks;
using BoardingHouseManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace BoardingHouseManagement.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly AppDbContext _context;

        public PaymentService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Invoice> GetInvoiceByIdAsync(Guid invoiceId)
        {
            return await _context.Invoices
                .Include(i => i.Contract)
                .ThenInclude(c => c.User) // Lấy thông tin người dùng nếu cần hiển thị
                .FirstOrDefaultAsync(i => i.Id == invoiceId);
        }

        public async Task<bool> ProcessPaymentAsync(Guid invoiceId, string paymentMethod)
        {
            var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId);
            if (invoice == null || invoice.Status == InvoiceStatus.Paid)
            {
                return false;
            }

            // Tạo bản ghi Payment
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoiceId,
                PaymentDate = DateTime.Now,
                Amount = invoice.TotalAmount,
                PaymentMethod = paymentMethod,
                TransactionNo = "TXN" + DateTime.Now.Ticks.ToString().Substring(0, 10) // Giả lập mã giao dịch
            };

            // Lưu payment
            _context.Payments.Add(payment);

            // Cập nhật trạng thái hoá đơn
            invoice.Status = InvoiceStatus.Paid;
            _context.Invoices.Update(invoice);

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
