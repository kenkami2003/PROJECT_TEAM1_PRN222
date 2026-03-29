using BoardingHouseManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PROJECT_TEAM1_PRN222.Controllers.Datmthe176706.Admin
{
    [Area("Datmthe176706")]
    public class ContractsController : Controller
    {
        private readonly AppDbContext _context;
        public ContractsController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View("~/Views/Datmthe176706/Admin/Index.cshtml");
        }

        public async Task<IActionResult> ContractList(Guid? buildingId, string searchTerm, DateTime? fromDate, DateTime? toDate)
        {
            ViewBag.Buildings = await _context.Buildings.AsNoTracking().ToListAsync();

            // Lưu lại giá trị lọc để hiển thị trên form
            ViewBag.SelectedBuilding = buildingId;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            var query = _context.Contracts
                .Include(c => c.Room)
                    .ThenInclude(r => r.Building)
                .Include(c => c.Tenan)
                .AsNoTracking()
                .AsQueryable();

            // Lọc theo Building
            if (buildingId.HasValue && buildingId != Guid.Empty)
            {
                query = query.Where(c => c.Room.BuildingId == buildingId);
            }

            // Lọc theo tìm kiếm (Tên, SĐT, Số phòng, CMND)
            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower().Trim();
                query = query.Where(c => c.Tenan.FullName.ToLower().Contains(searchTerm)
                                      || c.Tenan.Phone.Contains(searchTerm)
                                      || c.Tenan.IdentityNumber.Contains(searchTerm)
                                      || c.Room.RoomNumber.Contains(searchTerm));
            }

            // Lọc theo ngày bắt đầu hợp đồng
            if (fromDate.HasValue) query = query.Where(c => c.StartDate >= fromDate.Value);
            if (toDate.HasValue) query = query.Where(c => c.StartDate <= toDate.Value);

            var contracts = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();

            return View("~/Views/Datmthe176706/Admin/ContractList.cshtml", contracts);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateContract(Guid id)
        {
            var contract = await _context.Contracts
                .Include(c => c.Room)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contract == null) return NotFound();

            // Cập nhật trạng thái hợp đồng
            contract.IsActive = true;

            // Cập nhật trạng thái phòng sang "Đã thuê" (Occupied)
            if (contract.Room != null)
            {
                contract.Room.Status = RoomStatus.Occupied;
            }

            _context.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = contract.TenantId,
                Title = "Hợp đồng được kích hoạt",
                Message = $"Hợp đồng thuê phòng {(contract.Room?.RoomNumber ?? "không xác định")} của bạn đã được duyệt và bắt đầu có hiệu lực.",
                ActionLink = "/Datmthe176706/Users/MyRoom"
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã kích hoạt hợp đồng và cập nhật trạng thái phòng!";

            return RedirectToAction(nameof(ContractList));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TerminateContract(Guid id)
        {
            var contract = await _context.Contracts
                .Include(c => c.Room)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contract == null) return NotFound();

            // Giải phóng phòng về trạng thái Trống
            if (contract.Room != null)
            {
                contract.Room.Status = RoomStatus.Available; 

                // Xóa các chỉ số điện nước cũ của phòng này để khách mới không bị dính
                var oldReadings = await _context.UtilityReadings
                    .Where(ur => ur.RoomId == contract.RoomId)
                    .ToListAsync();
                
                if (oldReadings.Any())
                {
                    _context.UtilityReadings.RemoveRange(oldReadings);
                }
            }

            _context.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = contract.TenantId,
                Title = "Hủy Hợp đồng",
                Message = $"Hợp đồng thuê phòng {(contract.Room?.RoomNumber ?? "không xác định")} của bạn đã kết thúc hoặc bị hủy bỏ.",
                ActionLink = null
            });

            _context.Contracts.Remove(contract);

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã hủy hợp đồng, giải phóng phòng và làm sạch dữ liệu điện nước cũ thành công!";

            return RedirectToAction(nameof(ContractList));
        }

        [HttpGet]
        public async Task<IActionResult> Create(Guid reservationId)
        {
            var reservation = await _context.Reservations
                .Include(r => r.Guest)
                .Include(r => r.Room)
                .FirstOrDefaultAsync(r => r.Id == reservationId);

            if (reservation == null) return NotFound();

            ViewBag.Reservation = reservation;

            var model = new Contract
            {
                TenantId = reservation.GuestId,
                RoomId = reservation.RoomId,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddMonths(12),
                ActualDeposit = reservation.Room?.DepositAmount ?? 0
            };

            return View("~/Views/Datmthe176706/Admin/CreateContract.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Guid reservationId, Contract contract)
        {
            var reservation = await _context.Reservations
                .Include(r => r.Room)
                .FirstOrDefaultAsync(r => r.Id == reservationId);

            if (reservation == null) return NotFound();

            contract.Id = Guid.NewGuid();
            contract.ContractCode = "HD-" + DateTime.Now.ToString("yyyyMMdd") + "-" + Guid.NewGuid().ToString().Substring(0, 4).ToUpper();
            contract.IsActive = true;
            contract.CreatedAt = DateTime.Now;

            _context.Contracts.Add(contract);

            reservation.IsConvertedToContract = true;
            if (reservation.Room != null)
            {
                reservation.Room.Status = RoomStatus.Occupied;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã lập hợp đồng chính thức thành công!";

            return RedirectToAction(nameof(ContractList));
        }
        public async Task<IActionResult> ContractDetail(Guid id)
        {
            var contract = await _context.Contracts
                .Include(c => c.Room).ThenInclude(r => r.Building)
                .Include(c => c.Tenan) 
                .FirstOrDefaultAsync(m => m.Id == id);

            if (contract == null)
            {
                return NotFound();
            }

            return View("~/Views/Datmthe176706/Admin/ContractDetail.cshtml", contract);
        }
    }
}
