using BoardingHouseManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        public IActionResult Checkout(Guid roomId)
        {
            var room = _context.Rooms
                .Include(r => r.Contracts)
                .FirstOrDefault(r => r.Id == roomId);

            if (room == null) return NotFound();

            // Lấy contract đang active
            var contract = room.Contracts
                .FirstOrDefault(c => c.EndDate == null);

            ViewBag.HasContract = contract != null;

            return View("~/Views/Dungvthe171161/StaffDashboard/Checkout.cshtml", room);
        }
        [HttpPost]
        public IActionResult CheckoutConfirmed(Guid roomId)
        {
            var room = _context.Rooms
                .Include(r => r.Contracts)
                .FirstOrDefault(r => r.Id == roomId);

            if (room == null) return NotFound();

            var contract = room.Contracts
                .FirstOrDefault(c => c.EndDate == null);

            // ❗ Không có người thuê → không cho checkout
            if (contract == null)
            {
                return BadRequest("Phòng không có người thuê");
            }

            // ✅ Kết thúc hợp đồng
            contract.EndDate = DateTime.Now;

            // ✅ Cập nhật trạng thái phòng
            room.Status = RoomStatus.Available;

            // ✅ Ghi log (không dùng UserId để tránh lỗi)
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = Guid.NewGuid(), // fix tạm
                Action = "CHECKOUT",
                Details = $"Check-out phòng {room.RoomNumber}",
                Timestamp = DateTime.Now
            });

            _context.SaveChanges();

            // 🔥 QUAY VỀ DASHBOARD
            return RedirectToAction("Index");
        }
    }
}