using BoardingHouseManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PROJECT_TEAM1_PRN222.Controllers
{
    [Authorize]
    public class MaintenanceRequestController : Controller
    {
        private readonly AppDbContext _context;

        public MaintenanceRequestController(AppDbContext context)
        {
            _context = context;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var role = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            if (role?.ToLower() != "staff")
            {
                context.Result = new RedirectToActionResult("Index", "Login", new { area = "" });
            }
            base.OnActionExecuting(context);
        }

        // ===================== LIST =====================
        public async Task<IActionResult> Index()
        {
            var requests = await _context.MaintenanceRequests
                .Include(x => x.Room)
                .ToListAsync();

            return View("~/Views/Dungvthe171161/MaintenanceRequest/Index.cshtml", requests);
        }

        // ===================== CREATE GET =====================
        public IActionResult Create()
        {
            ViewBag.Rooms = _context.Rooms.ToList();

            return View("~/Views/Dungvthe171161/MaintenanceRequest/Create.cshtml");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MaintenanceRequest request)
        {
            // ✅ FIX ĐÚNG
            if (request.RoomId == Guid.Empty)
            {
                ModelState.AddModelError("RoomId", "Vui lòng chọn phòng");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Rooms = _context.Rooms.ToList();
                return View("~/Views/Dungvthe171161/MaintenanceRequest/Create.cshtml", request);
            }

            request.Id = Guid.NewGuid();
            request.TenantId = Guid.NewGuid();
            request.Status = RequestStatus.Open;
            request.CreatedAt = DateTime.Now;

            if (string.IsNullOrEmpty(request.ImageUrl))
            {
                request.ImageUrl = "";
            }

            _context.MaintenanceRequests.Add(request);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
        public async Task<IActionResult> Edit(Guid id)
        {
            var request = await _context.MaintenanceRequests.FindAsync(id);

            if (request == null)
            {
                return NotFound();
            }

            ViewBag.Rooms = _context.Rooms.ToList();

            return View("~/Views/Dungvthe171161/MaintenanceRequest/Edit.cshtml", request);
        }
        // ===================== EDIT POST =====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, MaintenanceRequest request)
        {
            if (id != request.Id)
            {
                return NotFound();
            }

            // validate Room
            if (request.RoomId == Guid.Empty)
            {
                ModelState.AddModelError("RoomId", "Vui lòng chọn phòng");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Rooms = _context.Rooms.ToList();
                return View("~/Views/Dungvthe171161/MaintenanceRequest/Edit.cshtml", request);
            }

            try
            {
                var existing = await _context.MaintenanceRequests.FindAsync(id);

                if (existing == null)
                {
                    return NotFound();
                }

                // update field
                existing.Title = request.Title;
                existing.Description = request.Description;
                existing.RoomId = request.RoomId;
                existing.ImageUrl = request.ImageUrl ?? "";
                existing.Status = request.Status;

                _context.Update(existing);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                throw;
            }

            return RedirectToAction("Index");
        }
        // ===================== DELETE =====================
        public async Task<IActionResult> Delete(Guid id)
        {
            var request = await _context.MaintenanceRequests.FindAsync(id);

            if (request != null)
            {
                _context.MaintenanceRequests.Remove(request);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var request = await _context.MaintenanceRequests.FindAsync(id);

            if (request != null)
            {
                _context.MaintenanceRequests.Remove(request);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

    }
}