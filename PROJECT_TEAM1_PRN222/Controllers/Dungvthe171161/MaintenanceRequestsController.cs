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
        public IActionResult Index(string status)
        {
            var query = _context.MaintenanceRequests
         .Include(x => x.Room)
         .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                var st = Enum.Parse<RequestStatus>(status);
                query = query.Where(x => x.Status == st);
            }

            var list = query.OrderByDescending(x => x.CreatedAt).ToList();

            return PartialView("~/Views/Dungvthe171161/MaintenanceRequests/Index.cshtml", list);
        }

        // CREATE
        public IActionResult Create()
        {
            // 🔥 1. PROPERTY
            if (!_context.Properties.Any())
            {
                _context.Properties.Add(new Property
                {
                    Id = Guid.NewGuid(),
                    Name = "Test Property",
                    Address = "Hà Nội" // ✅ FIX
                });

                _context.SaveChanges();
            }

            var property = _context.Properties.First();

            // 🔥 2. BUILDING
            if (!_context.Buildings.Any())
            {
                _context.Buildings.Add(new Building
                {
                    Id = Guid.NewGuid(),
                    Name = "Test Building",
                    PropertyId = property.Id // ✅ FIX
                });

                _context.SaveChanges();
            }

            var building = _context.Buildings.First();

            // 🔥 3. ROOM
            if (!_context.Rooms.Any())
            {
                _context.Rooms.Add(new Room
                {
                    Id = Guid.NewGuid(),
                    RoomNumber = "Test101",
                    BuildingId = building.Id, // ✅ FIX
                    Status = RoomStatus.Available,
                    Description = "Test room",
                    BasePrice = 1000000,
                    DepositAmount = 500000
                });

                _context.SaveChanges();
            }

            ViewBag.Rooms = _context.Rooms.ToList();

            return View("~/Views/Dungvthe171161/MaintenanceRequests/Create.cshtml");
        }

        [HttpPost]
        public IActionResult Create(MaintenanceRequest req)
        {
            ModelState.Remove("ImageUrl");
            ModelState.Remove("Room");

            // 🔥 FIX NULL Description
            if (string.IsNullOrWhiteSpace(req.Description))
            {
                req.Description = "Không có mô tả";
            }

            if (ModelState.IsValid)
            {
                req.Id = Guid.NewGuid();
                req.CreatedAt = DateTime.Now;
                req.Status = RequestStatus.Open;
                req.TenantId = Guid.NewGuid();

                req.ImageUrl = "no-image.png";

                // 🔥 FIX: đảm bảo RoomId có giá trị
                if (req.RoomId == Guid.Empty)
                {
                    return BadRequest("Room chưa được chọn");
                }

                _context.MaintenanceRequests.Add(req);

                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = req.TenantId,
                    Action = "CREATE_REQUEST",
                    Details = $"Tạo yêu cầu: {req.Title}",
                    Timestamp = DateTime.Now
                });

                _context.SaveChanges();

                return RedirectToAction("Index", "StaffDashboard");
            }

            ViewBag.Rooms = _context.Rooms.ToList();
            return View("~/Views/Dungvthe171161/MaintenanceRequests/Create.cshtml", req);
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
            var existing = _context.MaintenanceRequests.Find(req.Id);

            if (existing == null)
                return NotFound();

            // ✅ Chỉ update những field cần
            existing.Title = req.Title;
            existing.Description = req.Description;
            existing.Status = req.Status;

            // ⚠️ GIỮ nguyên Description nếu form không có
            // existing.Description = req.Description; // chỉ khi form có

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = existing.TenantId,
                Action = "UPDATE_REQUEST",
                Details = $"Cập nhật: {existing.Title} - {existing.Status}",
                Timestamp = DateTime.Now
            });

            _context.SaveChanges();

            return RedirectToAction("Index", "StaffDashboard");
        }
    }
}