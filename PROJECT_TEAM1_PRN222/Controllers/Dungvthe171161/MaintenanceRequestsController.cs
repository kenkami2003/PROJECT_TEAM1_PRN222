using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BoardingHouseManagement.Models;

namespace PROJECT_TEAM1_PRN222.Controllers.Dungvthe171161
{
    public class MaintenanceRequestsController : Controller
    {
        private readonly AppDbContext _context;

        public MaintenanceRequestsController(AppDbContext context)
        {
            _context = context;
        }

        // LIST
        public IActionResult Index()
        {
            var list = _context.MaintenanceRequests
                .Include(x => x.Room)
                .OrderByDescending(x => x.CreatedAt)
                .ToList();

            return View("~/Views/Dungvthe171161/MaintenanceRequests/Index.cshtml", list);
        }

        // CREATE
        public IActionResult Create()
        {
            ViewBag.Rooms = _context.Rooms.ToList();
            return View("~/Views/Dungvthe171161/MaintenanceRequests/Create.cshtml");
        }

        [HttpPost]
        public IActionResult Create(MaintenanceRequest req)
        {
            if (ModelState.IsValid)
            {
                req.Id = Guid.NewGuid();
                req.CreatedAt = DateTime.Now;
                req.Status = RequestStatus.Open;

                _context.MaintenanceRequests.Add(req);

                // 🔥 Audit Log
                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = req.TenantId,
                    Action = "CREATE_REQUEST",
                    Details = $"Tạo yêu cầu: {req.Title}",
                    Timestamp = DateTime.Now
                });

                _context.SaveChanges();

                return RedirectToAction("Index");
            }

            ViewBag.Rooms = _context.Rooms.ToList();
            return View(req);
        }

        // EDIT (staff xử lý)
        public IActionResult Edit(Guid id)
        {
            var req = _context.MaintenanceRequests.Find(id);
            return View("~/Views/Dungvthe171161/MaintenanceRequests/Edit.cshtml",req);
        }

        [HttpPost]
        public IActionResult Edit(MaintenanceRequest req)
        {
            _context.MaintenanceRequests.Update(req);

            // 🔥 Audit Log
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = req.TenantId,
                Action = "UPDATE_REQUEST",
                Details = $"Cập nhật: {req.Title} - {req.Status}",
                Timestamp = DateTime.Now
            });

            _context.SaveChanges();

            return RedirectToAction("Index");
        }
    }
}