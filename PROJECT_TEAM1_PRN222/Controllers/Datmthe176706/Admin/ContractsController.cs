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
            }

            _context.Contracts.Remove(contract);

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã hủy hợp đồng và giải phóng phòng thành công!";

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
