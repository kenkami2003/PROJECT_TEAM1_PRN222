using BoardingHouseManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PROJECT_TEAM1_PRN222.Controllers
{
    public class AuditLogsController : Controller
    {
        private readonly AppDbContext _context;

        public AuditLogsController(AppDbContext context)
        {
            _context = context;
        }

        // ===================== INDEX =====================
        public IActionResult Index()
        {
            var logs = _context.AuditLogs
                .OrderByDescending(x => x.Timestamp)
                .ToList();

            return View("~/Views/Dungvthe171161/AuditLogs/Index.cshtml", logs);
        }

        // ===================== TEST LOG =====================
        public IActionResult CreateTest()
        {
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = Guid.NewGuid(),
                Action = "TEST",
                Details = "Tạo thử log",
                Timestamp = DateTime.Now
            });

            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }

        // ===================== UPDATE STATUS + LOG =====================
        [HttpPost]
        public IActionResult UpdateStatus(Guid id, string status)
        {
            var request = _context.MaintenanceRequests
                                  .FirstOrDefault(x => x.Id == id);

            if (request == null)
                return NotFound();

            // ✅ convert string -> enum
            if (Enum.TryParse<RequestStatus>(status, out var newStatus))
            {
                request.Status = newStatus;
            }
            else
            {
                return BadRequest("Status không hợp lệ");
            }

            _context.SaveChanges();

            // ✅ log
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = Guid.NewGuid(),
                Action = "UPDATE_STATUS",
                Details = $"Request {id} -> {status}",
                Timestamp = DateTime.Now
            });

            _context.SaveChanges();

            return RedirectToAction("Index", "StaffDashboard");
        }
    }
}