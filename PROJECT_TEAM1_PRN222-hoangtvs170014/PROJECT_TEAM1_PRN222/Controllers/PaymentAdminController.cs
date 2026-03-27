using System;
using System.Linq;
using System.Threading.Tasks;
using BoardingHouseManagement.Services.Payment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PROJECT_TEAM1_PRN222.Controllers
{
    [Authorize]
    [Route("Admin/Payment")]
    public class PaymentAdminController : Controller
    {
        private readonly IPaymentService _paymentService;

        public PaymentAdminController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpGet("WaitingConfirmation")]
        public async Task<IActionResult> WaitingConfirmation()
        {
            var currentUserRole = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            if (currentUserRole?.ToLower() != "admin") return RedirectToAction("Index", "Home");

            var list = await _paymentService.GetWaitingConfirmationsAsync();
            return View("~/Views/Payment/WaitingConfirmation.cshtml", list);
        }

        [HttpPost("Confirm/{paymentId:guid}")]
        public async Task<IActionResult> Confirm(Guid paymentId)
        {
            var currentUserRole = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            if (currentUserRole?.ToLower() != "admin") return RedirectToAction("Index", "Home");

            await _paymentService.AdminConfirmAsync(paymentId);
            return RedirectToAction(nameof(WaitingConfirmation));
        }

        [HttpPost("Reject/{paymentId:guid}")]
        public async Task<IActionResult> Reject(Guid paymentId, string? note)
        {
            var currentUserRole = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            if (currentUserRole?.ToLower() != "admin") return RedirectToAction("Index", "Home");

            await _paymentService.AdminRejectAsync(paymentId, note);
            return RedirectToAction(nameof(WaitingConfirmation));
        }
    }
}

