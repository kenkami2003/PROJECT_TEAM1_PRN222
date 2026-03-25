using Microsoft.AspNetCore.Mvc;
using BoardingHouseManagement.Models;

namespace PROJECT_TEAM1_PRN222.Controllers.Dungvthe171161
{
    public class StaffDashboardController : Controller
    {
        private readonly AppDbContext _context;

        public StaffDashboardController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            ViewBag.TotalRequests = _context.MaintenanceRequests.Count();
            ViewBag.OpenRequests = _context.MaintenanceRequests.Count(x => x.Status == RequestStatus.Open);
            ViewBag.Processing = _context.MaintenanceRequests.Count(x => x.Status == RequestStatus.Processing);
            ViewBag.Fixed = _context.MaintenanceRequests.Count(x => x.Status == RequestStatus.Fixed);

            ViewBag.TotalLogs = _context.AuditLogs.Count();

            return View("~/Views/Dungvthe171161/StaffDashboard/Index.cshtml");
        }
    }
}