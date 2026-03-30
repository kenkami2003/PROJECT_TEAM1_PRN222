
using BoardingHouseManagement.Models;
using BoardingHouseManagement.Services.Admin;
using BoardingHouseManagement.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace PROJECT_TEAM1_PRN222.Controllers.Datmthe176706.Admin
{
    [Authorize(Roles = "Admin")]
    [Area("Datmthe176706")]
    [Route("Admin/AdminInvoices/[action]")]
    public class AdminInvoicesController : Controller
    {
        private readonly IAdminInvoiceService _invoiceService;

        public AdminInvoicesController(IAdminInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        [Route("/Admin/AdminInvoices")]
        [Route("/Admin/AdminInvoices/Index")]
        public async Task<IActionResult> Index(int? month = null, int? year = null, InvoiceStatus? status = null)
        {
            var invoices = await _invoiceService.GetAllAsync(month, year, status);
            var model = new AdminInvoiceListVM
            {
                Invoices = invoices,
                SelectedMonth = month,
                SelectedYear = year,
                SelectedStatus = status
            };
            return View("~/Views/Datmthe176706/Admin/AdminInvoices/Index.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var contracts = await _invoiceService.GetActiveContractsAsync();
            var model = new AdminInvoiceCreateVM
            {
                Month = DateTime.Now.Month,
                Year = DateTime.Now.Year,
                DueDate = DateTime.Now.AddDays(7), // Mặc định hạn 7 ngày
                Contracts = contracts.Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = $"{c.Tenan?.FullName} - Phòng {c.Room?.RoomNumber} ({c.ContractCode})"
                })
            };
            return View("~/Views/Datmthe176706/Admin/AdminInvoices/Create.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminInvoiceCreateVM model)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra trùng lặp (tận dụng luôn kiểm tra của hàm Suggestion)
                var checkDup = await _invoiceService.GetInvoiceSuggestionAsync(model.ContractId, model.Month, model.Year);
                if (checkDup.IsDuplicateInvoice)
                {
                    ModelState.AddModelError("", $"Lỗi: Hóa đơn của hợp đồng này trong tháng {model.Month}/{model.Year} đã tồn tại trong hệ thống. Vui lòng kiểm tra lại danh sách.");
                }
                // Xử lý cảnh báo thiếu điện nước
                if (!checkDup.HasUtilityReading)
                {
                    ModelState.AddModelError("", $"Cảnh báo: Hệ thống chưa chốt số điện / nước cho phòng này ở tháng {model.Month}/{model.Year}. Vui lòng chốt điện nước trước khi lập hóa đơn để tránh sai sót.");
                }
            }

            if (ModelState.IsValid)
            {
                var invoice = new Invoice
                {
                    ContractId = model.ContractId,
                    Month = model.Month,
                    Year = model.Year,
                    RoomAmount = model.RoomAmount,
                    UtilityAmount = model.UtilityAmount,
                    ServiceAmount = model.ServiceAmount,
                    PenaltyAmount = model.PenaltyAmount,
                    DueDate = model.DueDate,
                    InvoiceCode = "INV-" + DateTime.Now.Ticks.ToString().Substring(10), // Tự tạo mã ngắn gọn
                    Status = InvoiceStatus.Pending
                };

                if (await _invoiceService.CreateAsync(invoice))
                {
                    TempData["Success"] = "Tạo hóa đơn thành công!";
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError("", "Lỗi khi lưu vào cơ sở dữ liệu.");
            }

            // Reload contracts if failed
            var contracts = await _invoiceService.GetActiveContractsAsync();
            model.Contracts = contracts.Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = c.Id.ToString(),
                Text = $"{c.Tenan?.FullName} - Phòng {c.Room?.RoomNumber} ({c.ContractCode})"
            });
            return View("~/Views/Datmthe176706/Admin/AdminInvoices/Create.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var invoice = await _invoiceService.GetByIdAsync(id);
            if (invoice == null) return NotFound();

            if (invoice.Status == InvoiceStatus.Paid)
            {
                TempData["Error"] = "Không thể chỉnh sửa hóa đơn đã thanh toán!";
                return RedirectToAction(nameof(Index));
            }

            var model = new AdminInvoiceEditVM
            {
                Id = invoice.Id,
                InvoiceCode = invoice.InvoiceCode,
                TenantName = invoice.Contract?.Tenan?.FullName,
                RoomNumber = invoice.Contract?.Room?.RoomNumber,
                Month = invoice.Month,
                Year = invoice.Year,
                RoomAmount = invoice.RoomAmount,
                UtilityAmount = invoice.UtilityAmount,
                ServiceAmount = invoice.ServiceAmount,
                PenaltyAmount = invoice.PenaltyAmount,
                DueDate = invoice.DueDate,
                Status = invoice.Status
            };

            return View("~/Views/Datmthe176706/Admin/AdminInvoices/Edit.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AdminInvoiceEditVM model)
        {
            if (ModelState.IsValid)
            {
                var invoice = new Invoice
                {
                    Id = model.Id,
                    Month = model.Month,
                    Year = model.Year,
                    RoomAmount = model.RoomAmount,
                    UtilityAmount = model.UtilityAmount,
                    ServiceAmount = model.ServiceAmount,
                    PenaltyAmount = model.PenaltyAmount,
                    DueDate = model.DueDate,
                    Status = model.Status
                };

                if (await _invoiceService.UpdateAsync(invoice))
                {
                    TempData["Success"] = "Cập nhật hóa đơn thành công!";
                    return RedirectToAction(nameof(Index));
                }

                TempData["Error"] = "Cập nhật thất bại. Hãy kiểm tra lại hoặc liên hệ kỹ thuật.";
            }

            return View("~/Views/Datmthe176706/Admin/AdminInvoices/Edit.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> GetInvoiceSuggestion(Guid contractId, int month, int year)
        {
            if (contractId == Guid.Empty || month < 1 || month > 12 || year <= 0)
            {
                return BadRequest(new { message = "Dữ liệu đầu vào không hợp lệ (hợp đồng, tháng, năm)." });
            }

            var suggestion = await _invoiceService.GetInvoiceSuggestionAsync(contractId, month, year);
            return Json(suggestion);
        }

        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var invoice = await _invoiceService.GetByIdAsync(id);
            if (invoice == null) return NotFound();

            var model = new AdminInvoiceDetailsVM
            {
                Invoice = invoice
            };

            return View("~/Views/Datmthe176706/Admin/AdminInvoices/Details.cshtml", model);
        }
    }
}
