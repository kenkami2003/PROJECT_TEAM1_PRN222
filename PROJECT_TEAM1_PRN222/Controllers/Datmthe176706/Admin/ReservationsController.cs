using BoardingHouseManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PROJECT_TEAM1_PRN222.Controllers.Datmthe176706.Admin
{
    [Area("Datmthe176706")]
    public class ReservationsController : Controller
    {
       
        private readonly AppDbContext _context;
        public ReservationsController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View("~/Views/Datmthe176706/Admin/Index.cshtml");
        }
        public async Task<IActionResult> ReservationList(Guid? buildingId, ReservationStatus? status, string searchTerm, DateTime? fromDate, DateTime? toDate)
        {
            // Tự động dọn dẹp các đơn qúa hạn, chưa thanh toán
            await AutoCancelExpiredReservations();

            ViewBag.Buildings = await _context.Buildings.AsNoTracking().ToListAsync();

            // Lưu các giá trị lọc để hiển thị lại trên Form (ViewBag)
            ViewBag.SelectedBuilding = buildingId;
            ViewBag.SelectedStatus = status;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            var query = _context.Reservations
                .Include(r => r.Guest)
                .Include(r => r.Room)
                    .ThenInclude(room => room.Building)
                .AsNoTracking();

            // Lọc theo Building
            if (buildingId.HasValue && buildingId != Guid.Empty)
            {
                query = query.Where(r => r.Room.BuildingId == buildingId);
                ViewBag.SelectedBuilding = buildingId;
            }
            // Lọc theo Trạng thái
            if (status.HasValue)
            {
                query = query.Where(r => r.Status == status.Value);
            }

            // Lọc theo Tên hoặc Số điện thoại 
            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower().Trim();
                query = query.Where(r => r.Guest.FullName.ToLower().Contains(searchTerm)
                                      || r.Guest.Phone.Contains(searchTerm)
                                      || r.Room.RoomNumber.Contains(searchTerm));
            }

            // Lọc theo khoảng ngày đặt
            if (fromDate.HasValue)
            {
                query = query.Where(r => r.ReservedDate >= fromDate.Value);
            }
            if (toDate.HasValue)
            {
                var endOfDay = toDate.Value.AddDays(1);
                query = query.Where(r => r.ReservedDate < endOfDay);
            }

            var finalList = await query
                .OrderBy(r => r.Status) // Pending (0) sẽ lên trước Confirmed (1) và Cancelled (2)
                .ThenByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View("~/Views/Datmthe176706/Admin/ReservationsList.cshtml", finalList);
        }

        private async Task AutoCancelExpiredReservations()
        {
            var expiredItems = await _context.Reservations
                .Where(r => r.Status == ReservationStatus.Pending
                         && r.ExpiryDate < DateTime.Now
                         && !r.IsConvertedToContract)
                .Include(r => r.Room)
                .ToListAsync();

            if (expiredItems.Any())
            {
                foreach (var item in expiredItems)
                {
                    item.Status = ReservationStatus.Cancelled;
                    item.Note += " [Hệ thống tự động hủy do quá hạn]";

                    // Trả trạng thái phòng về Trống (Available = 0)
                    if (item.Room != null)
                    {
                        item.Room.Status = 0;
                    }
                }
                await _context.SaveChangesAsync();
            }
        }
        public async Task<IActionResult> Details(Guid id)
        {
            var reservation = await _context.Reservations
                .Include(r => r.Guest)
                .Include(r => r.Room)
                    .ThenInclude(room => room.Building)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (reservation == null) return NotFound();

            return View("~/Views/Datmthe176706/Admin/ReservationDetail.cshtml", reservation);
        }

        [HttpPost]
        public async Task<IActionResult> CancelReservation(Guid id)
        {
            var reservation = await _context.Reservations
                .Include(r => r.Room)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin giữ chỗ.";
                return RedirectToAction("ReservationList");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (reservation.Room != null)
                {
                    reservation.Room.Status = 0; // 0: Available/Trống
                }

                _context.Notifications.Add(new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = reservation.GuestId,
                    Title = "Hủy Giữ chỗ",
                    Message = $"Yêu cầu giữ phòng {(reservation.Room?.RoomNumber ?? "không xác định")} của bạn đã bị hủy/từ chối.",
                    ActionLink = null
                });

                _context.Reservations.Remove(reservation);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = "Đã hủy giữ chỗ và giải phóng phòng thành công.";
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Có lỗi xảy ra khi hủy giữ chỗ.";
            }

            return RedirectToAction("ReservationList");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmReservation(Guid id)
        {
            var reservation = await _context.Reservations
                .Include(r => r.Room)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
            {
                TempData["Error"] = "Không tìm thấy đơn giữ chỗ.";
                return RedirectToAction("Index");
            }

            try
            {
                reservation.Status = ReservationStatus.Confirmed;

                _context.Update(reservation);

                _context.Notifications.Add(new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = reservation.GuestId,
                    Title = "Xác nhận Giữ chỗ",
                    Message = $"Yêu cầu giữ phòng {(reservation.Room?.RoomNumber ?? "không xác định")} của bạn đã được duyệt.",
                    ActionLink = null
                });

                await _context.SaveChangesAsync();

                TempData["Success"] = "Đã phê duyệt đơn giữ chỗ thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi hệ thống: " + ex.Message;
            }

            return RedirectToAction("Details", new { id = id });
        }
      
    }
}
