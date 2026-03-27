using System;
using System.Linq;
using System.Threading.Tasks;
using BoardingHouseManagement.Services.Invoice;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PROJECT_TEAM1_PRN222.Controllers
{
    [Authorize]
    [Route("Admin/Invoices")]
    public class InvoiceAdminController : Controller
    {
        private readonly IInvoiceAdminService _invoiceAdminService;

        public InvoiceAdminController(IInvoiceAdminService invoiceAdminService)
        {
            _invoiceAdminService = invoiceAdminService;
        }

        private bool IsAdmin()
        {
            var role = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            return string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase);
        }

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            var list = await _invoiceAdminService.GetInvoicesAsync();
            return View("~/Views/InvoiceAdmin/Index.cshtml", list);
        }

        [HttpGet("Create")]
        public async Task<IActionResult> Create()
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            var model = await _invoiceAdminService.GetEmptyForCreateAsync();
            return View("~/Views/InvoiceAdmin/Edit.cshtml", model);
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create(InvoiceEditDto model)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            model.Contracts = (await _invoiceAdminService.GetActiveContractsForSelectAsync());
            try
            {
                await _invoiceAdminService.CreateAsync(model);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View("~/Views/InvoiceAdmin/Edit.cshtml", model);
            }
        }

        [HttpGet("Edit/{id:guid}")]
        public async Task<IActionResult> Edit(Guid id)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            try
            {
                var model = await _invoiceAdminService.GetForEditAsync(id);
                if (model == null) return NotFound();
                return View("~/Views/InvoiceAdmin/Edit.cshtml", model);
            }
            catch (Exception ex)
            {
                TempData["InvoiceWarning"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost("Edit/{id:guid}")]
        public async Task<IActionResult> Edit(Guid id, InvoiceEditDto model)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            model.Id = id;
            model.Contracts = (await _invoiceAdminService.GetActiveContractsForSelectAsync());
            try
            {
                await _invoiceAdminService.UpdateAsync(model);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View("~/Views/InvoiceAdmin/Edit.cshtml", model);
            }
        }
    }
}
