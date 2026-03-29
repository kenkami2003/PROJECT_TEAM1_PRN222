using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BoardingHouseManagement.Services;
using BoardingHouseManagement.ViewModel;
using BoardingHouseManagement.Models;
using Microsoft.AspNetCore.Authorization;
using BoardingHouseManagement.Services.VnPay;

namespace BoardingHouseManagement.Controllers.Hoangtvse170014
{
    [Authorize] // Phải đăng nhập mới được thao tác thanh toán
    public class PaymentController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly IVnPayService _vnPayService;

        public PaymentController(IPaymentService paymentService, IVnPayService vnPayService)
        {
            _paymentService = paymentService;
            _vnPayService = vnPayService;
        }

        // GET: /Payment/Index/{invoiceId}
        [HttpGet]
        public async Task<IActionResult> Index(Guid id)
        {
            var invoice = await _paymentService.GetInvoiceByIdAsync(id);
            if (invoice == null)
            {
                return NotFound("Hoá đơn không tồn tại.");
            }

            if (invoice.Status == InvoiceStatus.Paid)
            {
                ViewBag.Message = "Hoá đơn này đã được thanh toán rồi.";
            }

            var vm = new PaymentViewModel
            {
                InvoiceId = invoice.Id,
                InvoiceCode = invoice.InvoiceCode,
                TotalAmount = invoice.TotalAmount,
                Month = invoice.Month,
                Year = invoice.Year,
                Username = invoice.Contract?.Tenan?.Username ?? "Khách hệ thống"
            };

            return View("~/Views/Hoangtvse170014/Payment/Index.cshtml", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Process(Guid invoiceId)
        {
            // Kiểm tra Auth nếu project có
            string currentUsername = User.Identity?.Name ?? "";

            // Tránh nhồi logic vào Controller: uỷ quyền Service làm hết
            var payment = await _paymentService.CreatePendingVnPayPaymentAsync(invoiceId, currentUsername);
            
            // Service trả null khi (1) Hoá đơn không tồn tại, (2) Đã Paid, (3) Không thuộc về User này.
            if (payment == null) 
            {
                TempData["ErrorMessage"] = "Hoá đơn không hợp lệ, đã được thanh toán hoặc bạn không có quyền!";
                return RedirectToAction("MyInvoices", "Users", new { area = "Datmthe176706" }); 
            }

            // Build URL sang VNPAY sandbox
            var paymentUrl = _vnPayService.CreatePaymentUrl(payment, HttpContext);
            return Redirect(paymentUrl);
        }

        [HttpGet]
        [AllowAnonymous] // Cho phép VNPAY gọi về mà không cần check login
        public async Task<IActionResult> PaymentCallback()
        {
            // Tránh business logic dài: uỷ quyền Service extract và verify url, hash, Db
            var (isSuccess, invoiceId) = await _paymentService.HandleVnPayReturnAsync(Request.Query);

            string vnp_ResponseCode = Request.Query["vnp_ResponseCode"];

            if (isSuccess && vnp_ResponseCode == "00")
            {
                TempData["SuccessMessage"] = "Thanh toán thành công qua VNPAY!";
            }
            else
            {
                if (vnp_ResponseCode == "24") // 24 = Cancelled
                {
                    TempData["ErrorMessage"] = "Bạn đã huỷ giao dịch thanh toán VNPAY.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Giao dịch VNPAY thất bại, mã lỗi: " + vnp_ResponseCode;
                }
            }

            // Nếu lấy được hoá đơn, chuyển về chi tiết hoá đơn đó
            return RedirectToAction("MyInvoices", "Users", new { area = "Datmthe176706" }); 
        }
    }
}
