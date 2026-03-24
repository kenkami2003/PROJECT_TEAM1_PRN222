using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BoardingHouseManagement.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

public class UtilityController : Controller
{
    private readonly AppDbContext _context;
    public UtilityController(AppDbContext context) => _context = context;

    public async Task<IActionResult> CreateBatch(Guid? propertyId, int? month, int? year)
    {
        int sMonth = month ?? DateTime.Now.Month;
        int sYear = year ?? DateTime.Now.Year;

        ViewBag.PropertyList = new SelectList(await _context.Properties.ToListAsync(), "Id", "Name", propertyId);
        var viewModel = new UtilityBatchViewModel { Month = sMonth, Year = sYear, PropertyId = propertyId ?? Guid.Empty };

        if (propertyId.HasValue)
        {
            var property = await _context.Properties
                .Include(p => p.Buildings).ThenInclude(b => b.Rooms).ThenInclude(r => r.UtilityReadings)
                .FirstOrDefaultAsync(p => p.Id == propertyId);

            if (property != null)
            {
                viewModel.PropertyName = property.Name;
                foreach (var building in property.Buildings)
                {
                    foreach (var room in building.Rooms.OrderBy(r => r.RoomNumber))
                    {
                        // 1. Tìm bản ghi của CHÍNH THÁNG/NĂM ĐANG CHỌN (để lấy Số Mới nếu đã lưu)
                        var currentReading = room.UtilityReadings
                            .FirstOrDefault(u => u.Month == sMonth && u.Year == sYear);

                        // 2. Tìm bản ghi của THÁNG TRƯỚC ĐÓ (để lấy Số Cũ)
                        // Logic: Tìm bản ghi có (Year < sYear) HOẶC (Year == sYear và Month < sMonth)
                        var lastReading = room.UtilityReadings
                            .Where(u => u.Year < sYear || (u.Year == sYear && u.Month < sMonth))
                            .OrderByDescending(u => u.Year).ThenByDescending(u => u.Month)
                            .FirstOrDefault();

                        viewModel.Rooms.Add(new UtilityEntryItem
                        {
                            RoomId = room.Id,
                            RoomNumber = room.RoomNumber,
                            BuildingName = building.Name,
                            BasePrice = room.BasePrice,
                            ServiceFee = property.ServiceFee,
                            PriceUnitElectricity = property.PriceUnitElectricity,
                            PriceUnitWater = property.PriceUnitWater,

                            // Số cũ luôn lấy từ tháng trước
                            OldElectricity = lastReading?.NewElectricity ?? 0,
                            OldWater = lastReading?.NewWater ?? 0,

                            // Số mới: Nếu tháng này đã lưu thì hiện số đã lưu, nếu chưa thì để 0 (hoặc để trống)
                            NewElectricity = currentReading?.NewElectricity ?? 0,
                            NewWater = currentReading?.NewWater ?? 0
                        });
                    }
                }
            }
        }
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveBatch(UtilityBatchViewModel model)
    {
        // Kiểm tra xem model có nhận được danh sách Rooms từ View gửi về không
        if (model.Rooms == null || !model.Rooms.Any())
        {
            TempData["Error"] = "Không nhận được dữ liệu phòng nào!";
            return RedirectToAction("CreateBatch", new { propertyId = model.PropertyId, month = model.Month, year = model.Year });
        }

        foreach (var item in model.Rooms)
        {
            // Quan trọng: Phải tìm bản ghi dựa trên RoomId, Month, Year từ Model gửi về
            var existingReading = await _context.UtilityReadings
                .FirstOrDefaultAsync(r => r.RoomId == item.RoomId && r.Month == model.Month && r.Year == model.Year);

            if (existingReading != null)
            {
                // Nếu đã có: Cập nhật (Update)
                existingReading.NewElectricity = item.NewElectricity;
                existingReading.NewWater = item.NewWater;
                existingReading.ReadingDate = DateTime.Now;
                _context.Update(existingReading);
            }
            else
            {
                // Nếu chưa có: Thêm mới (Insert)
                var newReading = new UtilityReading
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
                    // RecordedBy = ... (ID người dùng nếu có)
                };
                _context.Add(newReading);
            }
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = "Đã lưu dữ liệu thành công!";

        // Quay lại đúng vị trí để kiểm tra
        return RedirectToAction("CreateBatch", new
        {
            propertyId = model.PropertyId,
            month = model.Month,
            year = model.Year
        });
    }
}