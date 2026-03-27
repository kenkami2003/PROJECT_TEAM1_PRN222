using BoardingHouseManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PROJECT_TEAM1_PRN222.Controllers
{
    public class StaffDashboardController : Controller
    {
        private readonly AppDbContext _context;

        public StaffDashboardController(AppDbContext context)
        {
            _context = context;
        }

        // ===================== DASHBOARD =====================
        public IActionResult Index(string status)
        {
            var query = _context.MaintenanceRequests
    .Include(x => x.Room)
    .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status)
    && Enum.TryParse<RequestStatus>(status.Trim(), true, out var statusEnum))
            {
                query = query.Where(x => x.Status == statusEnum);
            }
            

            var list = query
                .OrderByDescending(x => x.CreatedAt)
                .AsNoTracking()
                .ToList();

            ViewBag.TotalRequests = _context.MaintenanceRequests.Count();
            ViewBag.OpenRequests = _context.MaintenanceRequests.Count(x => x.Status == RequestStatus.Open);
            ViewBag.Processing = _context.MaintenanceRequests.Count(x => x.Status == RequestStatus.Processing);
            ViewBag.Fixed = _context.MaintenanceRequests.Count(x => x.Status == RequestStatus.Fixed);

            ViewBag.TotalLogs = _context.AuditLogs.Count();

            return View("~/Views/Dungvthe171161/StaffDashboard/Index.cshtml", list);
        }


        [HttpPost]
        public async Task<IActionResult> UpdateStatus(Guid id, string status)
        {
            var request = await _context.MaintenanceRequests.FindAsync(id);

            if (request == null)
            {
                return NotFound();
            }

            // FIX CỨNG - KHÔNG BAO GIỜ FAIL
            switch (status)
            {
                case "Processing":
                    request.Status = RequestStatus.Processing;
                    break;

                case "Fixed":
                    request.Status = RequestStatus.Fixed;
                    break;

                default:
                    request.Status = RequestStatus.Open;
                    break;
            }
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = Guid.NewGuid(),
                Action = "UPDATE_STATUS",
                Details = $"Request {id} -> {status}",
                Timestamp = DateTime.Now
            });
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

    }
}