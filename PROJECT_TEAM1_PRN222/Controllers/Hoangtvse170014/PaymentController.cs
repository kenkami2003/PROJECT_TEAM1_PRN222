using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BoardingHouseManagement.Services;
using BoardingHouseManagement.ViewModel;

namespace BoardingHouseManagement.Controllers.Hoangtvse170014
{
    public class PaymentController : Controller
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
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

            if (invoice.Status == Models.InvoiceStatus.Paid)
            {
                // Có thể điều hướng về trang chi tiết thông báo đã thanh toán
                ViewBag.Message = "Hoá đơn này đã được thanh toán rồi.";
            }

            var vm = new PaymentViewModel
            {
                InvoiceId = invoice.Id,
                InvoiceCode = invoice.InvoiceCode,
                TotalAmount = invoice.TotalAmount,
                Month = invoice.Month,
                Year = invoice.Year,
                Username = invoice.Contract?.User?.Username ?? "N/A"
            };

            // Trỏ tới đúng file view để không bị MVC mò mẫm sai vị trí
            return View("~/Views/Hoangtvse170014/Payment/Index.cshtml", vm);
        }

        // POST: /Payment/Process
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Process(PaymentViewModel model)
        {
            if (string.IsNullOrEmpty(model.PaymentMethod))
            {
                ModelState.AddModelError("PaymentMethod", "Vui lòng chọn phương thức thanh toán.");
                return View("~/Views/Hoangtvse170014/Payment/Index.cshtml", model);
            }

            var success = await _paymentService.ProcessPaymentAsync(model.InvoiceId, model.PaymentMethod);

            if (success)
            {
                // Thanh toán thành công, hiển thị về UI (redirect về dashboard hoặc details)
                TempData["SuccessMessage"] = "Thanh toán thành công hoá đơn " + model.InvoiceCode;
                // Có thể redirect về trang gốc (home hoặc invoice detail), tạm thời redirect về Index (hoặc trang success riêng tuỳ bạn)
                return RedirectToAction("Index", new { id = model.InvoiceId });
            }

            ModelState.AddModelError("", "Thanh toán thất bại. Trạng thái hoá đơn có thể đã thay đổi hoặc lỗi hệ thống.");
            return View("~/Views/Hoangtvse170014/Payment/Index.cshtml", model);
        }
    }
}
