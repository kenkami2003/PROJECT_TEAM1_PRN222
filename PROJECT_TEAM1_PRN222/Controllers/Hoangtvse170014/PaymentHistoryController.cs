using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BoardingHouseManagement.Services;
using BoardingHouseManagement.Models;

namespace BoardingHouseManagement.Controllers.Hoangtvse170014
{
    [Authorize(Roles = "Admin")] // Ngăn chặn Tenant/User thường access trang quản trị
    public class PaymentHistoryController : Controller
    {
        private readonly IPaymentService _paymentService;

        public PaymentHistoryController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        // GET: /PaymentHistory/Index?status=Success
        public async Task<IActionResult> Index(PaymentStatus? status)
        {
            // Không nhồi logic query vào Controller, tôi lấy qua Service
            var payments = await _paymentService.GetAllPaymentsAsync(status);

            // Truyền trạng thái lọc hiện tại về View để hiển thị Active
            ViewData["CurrentFilter"] = status;

            return View("~/Views/Hoangtvse170014/PaymentHistory/Index.cshtml", payments);
        }
    }
}
