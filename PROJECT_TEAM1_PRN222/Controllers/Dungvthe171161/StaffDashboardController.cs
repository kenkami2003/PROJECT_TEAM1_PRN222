using BoardingHouseManagement.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
        public async Task<IActionResult> Index(string status, Guid? propertyId, int? month, int? year)
        {
            // ===== 1. MAINTENANCE =====
            var query = _context.MaintenanceRequests
                .Include(x => x.Room)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status)
                && Enum.TryParse<RequestStatus>(status.Trim(), true, out var statusEnum))
            {
                query = query.Where(x => x.Status == statusEnum);
            }

            var list = await query
                .OrderByDescending(x => x.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.TotalRequests = _context.MaintenanceRequests.Count();
            ViewBag.OpenRequests = _context.MaintenanceRequests.Count(x => x.Status == RequestStatus.Open);
            ViewBag.Processing = _context.MaintenanceRequests.Count(x => x.Status == RequestStatus.Processing);
            ViewBag.Fixed = _context.MaintenanceRequests.Count(x => x.Status == RequestStatus.Fixed);
            ViewBag.TotalLogs = _context.AuditLogs.Count();

            // ===== 2. UTILITY =====
            int sMonth = month ?? DateTime.Now.Month;
            int sYear = year ?? DateTime.Now.Year;

            ViewBag.PropertyList = new SelectList(await _context.Properties.ToListAsync(), "Id", "Name", propertyId);

            var utilityVM = await BuildUtilityVM(propertyId, sMonth, sYear);

            ViewBag.UtilityVM = utilityVM;

            return View("~/Views/Dungvthe171161/StaffDashboard/Index.cshtml", list);
        }

        // ===================== BUILD UTILITY =====================
        private async Task<UtilityBatchViewModel> BuildUtilityVM(Guid? propertyId, int month, int year)
        {
            var vm = new UtilityBatchViewModel
            {
                Month = month,
                Year = year,
                PropertyId = propertyId ?? Guid.Empty
            };

            if (!propertyId.HasValue) return vm;

            var property = await _context.Properties
                .Include(p => p.Buildings)
                .ThenInclude(b => b.Rooms)
                .ThenInclude(r => r.UtilityReadings)
                .FirstOrDefaultAsync(p => p.Id == propertyId);

            if (property == null) return vm;

            foreach (var building in property.Buildings)
            {
                foreach (var room in building.Rooms.OrderBy(r => r.RoomNumber))
                {
                    var current = room.UtilityReadings
                        .FirstOrDefault(u => u.Month == month && u.Year == year);

                    var last = room.UtilityReadings
                        .Where(u => u.Year < year || (u.Year == year && u.Month < month))
                        .OrderByDescending(u => u.Year)
                        .ThenByDescending(u => u.Month)
                        .FirstOrDefault();

                    vm.Rooms.Add(new UtilityEntryItem
                    {
                        RoomId = room.Id,
                        RoomNumber = room.RoomNumber,
                        BuildingName = building.Name,
                        BasePrice = room.BasePrice,
                        ServiceFee = property.ServiceFee,
                        PriceUnitElectricity = property.PriceUnitElectricity,
                        PriceUnitWater = property.PriceUnitWater,
                        OldElectricity = last?.NewElectricity ?? 0,
                        OldWater = last?.NewWater ?? 0,
                        NewElectricity = current?.NewElectricity ?? 0,
                        NewWater = current?.NewWater ?? 0
                    });
                }
            }

            return vm;
        }

        // ===================== UPDATE STATUS =====================
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(Guid id, string status)
        {
            var request = await _context.MaintenanceRequests.FindAsync(id);
            if (request == null) return NotFound();

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
        public async Task<IActionResult> Utility(Guid? propertyId, int? month, int? year)
        {
            int sMonth = month ?? DateTime.Now.Month;
            int sYear = year ?? DateTime.Now.Year;

            ViewBag.PropertyList = new SelectList(
                await _context.Properties.ToListAsync(), "Id", "Name", propertyId);

            var vm = await BuildUtilityVM(propertyId, sMonth, sYear);

            return View("~/Views/Dungvthe171161/StaffDashboard/Utility.cshtml", vm);
        }
        // ===================== SAVE UTILITY =====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveBatch(UtilityBatchViewModel model)
        {
            if (model.Rooms == null || !model.Rooms.Any())
            {
                TempData["Error"] = "Không có dữ liệu!";
                return RedirectToAction("Utility", new
                {
                    propertyId = model.PropertyId,
                    month = model.Month,
                    year = model.Year
                });
            }

            foreach (var item in model.Rooms)
            {
                var existing = await _context.UtilityReadings
                    .FirstOrDefaultAsync(x => x.RoomId == item.RoomId
                                          && x.Month == model.Month
                                          && x.Year == model.Year);

                if (existing != null)
                {
                    existing.NewElectricity = item.NewElectricity;
                    existing.NewWater = item.NewWater;
                    existing.ReadingDate = DateTime.Now;
                }
                else
                {
                    _context.UtilityReadings.Add(new UtilityReading
                    {
                        Id = Guid.NewGuid(),
                        RoomId = item.RoomId,
                        Month = model.Month,
                        Year = model.Year,
                        OldElectricity = item.OldElectricity,
                        NewElectricity = item.NewElectricity,
                        OldWater = item.OldWater,
                        NewWater = item.NewWater,
                        ReadingDate = DateTime.Now
                    });
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Index", new
            {
                propertyId = model.PropertyId,
                month = model.Month,
                year = model.Year
            });
        }
    }
}