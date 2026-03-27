using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BoardingHouseManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROJECT_TEAM1_PRN222.ViewModels.Payment;

namespace PROJECT_TEAM1_PRN222.Controllers
{
    [Authorize]
    [Route("Admin/PaymentConfig")]
    public class PaymentConfigAdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public PaymentConfigAdminController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
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

            var current = await _context.PaymentReceiverConfigs
                .OrderByDescending(x => x.UpdatedAt)
                .FirstOrDefaultAsync();

            var model = new PaymentReceiverConfigViewModel();
            if (current != null)
            {
                model.Id = current.Id;
                model.BankCode = current.BankCode;
                model.AccountNumber = current.AccountNumber;
                model.AccountName = current.AccountName;
                model.BranchName = current.BranchName;
                model.IsActive = current.IsActive;
                model.UpdatedAt = current.UpdatedAt;
                model.QrImageRelativePath = current.QrImageRelativePath;
            }

            return View("~/Views/PaymentConfig/Index.cshtml", model);
        }

        [HttpPost("Save")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> Save(PaymentReceiverConfigViewModel model, IFormFile? qrImage)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            if (!ModelState.IsValid)
            {
                var reload = await _context.PaymentReceiverConfigs
                    .AsNoTracking()
                    .OrderByDescending(x => x.UpdatedAt)
                    .FirstOrDefaultAsync();
                model.QrImageRelativePath = reload?.QrImageRelativePath;
                return View("~/Views/PaymentConfig/Index.cshtml", model);
            }

            var existing = await _context.PaymentReceiverConfigs
                .OrderByDescending(x => x.UpdatedAt)
                .FirstOrDefaultAsync();

            if (existing == null)
            {
                existing = new PaymentReceiverConfig { Id = Guid.NewGuid() };
                _context.PaymentReceiverConfigs.Add(existing);
            }

            existing.BankCode = model.BankCode.Trim().ToUpperInvariant();
            existing.AccountNumber = model.AccountNumber.Trim();
            existing.AccountName = model.AccountName.Trim();
            existing.BranchName = string.IsNullOrWhiteSpace(model.BranchName) ? null : model.BranchName.Trim();
            existing.IsActive = model.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;

            if (qrImage != null && qrImage.Length > 0)
            {
                var ext = Path.GetExtension(qrImage.FileName)?.ToLowerInvariant() ?? string.Empty;
                if (ext != ".png" && ext != ".jpg" && ext != ".jpeg" && ext != ".webp" && ext != ".gif")
                {
                    ModelState.AddModelError(string.Empty, "Chi chap nhan file anh: PNG, JPG, JPEG, WEBP hoac GIF.");
                    model.QrImageRelativePath = existing.QrImageRelativePath;
                    return View("~/Views/PaymentConfig/Index.cshtml", model);
                }

                var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "bank-qr");
                Directory.CreateDirectory(uploadDir);

                if (!string.IsNullOrEmpty(existing.QrImageRelativePath))
                    TryDeleteQrFile(existing.QrImageRelativePath);

                var fileName = $"{Guid.NewGuid():N}{ext}";
                var physicalPath = Path.Combine(uploadDir, fileName);
                await using (var stream = System.IO.File.Create(physicalPath))
                    await qrImage.CopyToAsync(stream);

                existing.QrImageRelativePath = "/uploads/bank-qr/" + fileName;
            }

            await _context.SaveChangesAsync();

            TempData["PaymentConfigMessage"] = "Da luu cau hinh nhan tien.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("RemoveQr")]
        public async Task<IActionResult> RemoveQr()
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            var existing = await _context.PaymentReceiverConfigs
                .OrderByDescending(x => x.UpdatedAt)
                .FirstOrDefaultAsync();

            if (existing != null && !string.IsNullOrEmpty(existing.QrImageRelativePath))
            {
                TryDeleteQrFile(existing.QrImageRelativePath);
                existing.QrImageRelativePath = null;
                existing.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            TempData["PaymentConfigMessage"] = "Da xoa anh QR.";
            return RedirectToAction(nameof(Index));
        }

        private void TryDeleteQrFile(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return;
            var norm = relativePath.Replace('\\', '/').TrimStart('/');
            if (!norm.StartsWith("uploads/bank-qr/", StringComparison.OrdinalIgnoreCase)) return;
            var full = Path.GetFullPath(Path.Combine(_env.WebRootPath, norm.Replace('/', Path.DirectorySeparatorChar)));
            var root = Path.GetFullPath(Path.Combine(_env.WebRootPath, "uploads", "bank-qr"));
            if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(full)) return;
            try { System.IO.File.Delete(full); } catch { }
        }
    }
}
