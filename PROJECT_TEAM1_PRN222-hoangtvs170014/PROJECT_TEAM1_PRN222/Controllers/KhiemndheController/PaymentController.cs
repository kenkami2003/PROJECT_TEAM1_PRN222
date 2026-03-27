using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BoardingHouseManagement.Models;
using BoardingHouseManagement.Services.Payment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PROJECT_TEAM1_PRN222.Controllers.KhiemndheController
{
    [Authorize]
    [Route("Payment")]
    public class PaymentController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly AppDbContext _db;

        public PaymentController(IPaymentService paymentService, AppDbContext db)
        {
            _paymentService = paymentService;
            _db = db;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            return Content("Payment module is running. Use /Payment/Invoice/{invoiceId}");
        }

        [HttpGet("MyInvoices")]
        public async Task<IActionResult> MyInvoices()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return RedirectToAction("Index", "Login");
            }

            var invoices = await _paymentService.GetTenantInvoicesAsync(userId);
            return View("~/Views/Khiemndhe/Payment/MyInvoices.cshtml", invoices);
        }

        [HttpGet("Invoice/{invoiceId:guid}")]
        public async Task<IActionResult> InvoicePayment(Guid invoiceId)
        {
            var result = await _paymentService.CreateOrGetPaymentAsync(invoiceId);
            var cfg = await _db.PaymentReceiverConfigs
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.UpdatedAt)
                .FirstOrDefaultAsync();
            ViewBag.ReceiverBankCode = cfg?.BankCode;
            ViewBag.ReceiverAccountNumber = cfg?.AccountNumber;
            ViewBag.ReceiverAccountName = cfg?.AccountName;
            ViewBag.ReceiverBranchName = cfg?.BranchName;
            ViewBag.StaticQrUrl = cfg?.QrImageRelativePath;
            ViewBag.TenantEmail = User.FindFirstValue(ClaimTypes.Email)
                ?? User.FindFirstValue(ClaimTypes.Name)
                ?? User.Identity?.Name;
            return View("~/Views/Khiemndhe/Payment/InvoicePayment.cshtml", result);
        }

        // Compatibility route: some clients/proxies may fail with :guid constraint.
        [HttpGet("Invoice/{invoiceId}")]
        public async Task<IActionResult> InvoicePaymentRaw(string invoiceId)
        {
            if (!Guid.TryParse(invoiceId, out var parsedInvoiceId))
            {
                return BadRequest("Invalid invoiceId format.");
            }

            return await InvoicePayment(parsedInvoiceId);
        }

        [HttpPost("Customer/MarkPaid/{paymentId:guid}")]
        public async Task<IActionResult> CustomerMarkPaid(Guid paymentId)
        {
            var payment = await _paymentService.CustomerMarkPaidAsync(paymentId);

            return RedirectToAction(
                nameof(InvoicePayment),
                new { invoiceId = payment.InvoiceId }
            );
        }

        // Test-friendly GET endpoint to avoid browser/form method issues.
        [HttpGet("Customer/MarkPaid/{paymentId}")]
        public async Task<IActionResult> CustomerMarkPaidGet(string paymentId)
        {
            if (!Guid.TryParse(paymentId, out var parsedPaymentId))
            {
                return BadRequest("Invalid paymentId format.");
            }

            var payment = await _paymentService.CustomerMarkPaidAsync(parsedPaymentId);

            return RedirectToAction(
                nameof(InvoicePayment),
                new { invoiceId = payment.InvoiceId }
            );
        }
    }
}

