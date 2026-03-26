using BoardingHouseManagement.Models;
using Microsoft.AspNetCore.Mvc;

namespace PROJECT_TEAM1_PRN222.Controllers.Dungvthe171161
{
    public class AuditLogsController : Controller
    {
        private readonly AppDbContext _context;

        public AuditLogsController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var logs = _context.AuditLogs
                .OrderByDescending(x => x.Timestamp)
                .ToList();

            return View("~/Views/Dungvthe171161/AuditLogs/Index.cshtml", logs);
        }

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
    }
}