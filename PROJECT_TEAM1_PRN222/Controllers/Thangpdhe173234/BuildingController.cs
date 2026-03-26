using BoardingHouseManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PROJECT_TEAM1_PRN222.Controllers.Thangpdhe173234
{
    public class BuildingController : Controller
    {
        private readonly AppDbContext _context;

        public BuildingController(AppDbContext context)
        {
            _context = context;
        }

        // INDEX
        public IActionResult Index(Guid? propertyId)
        {
            var query = _context.Properties
                .Include(p => p.Buildings)
                    .ThenInclude(b => b.Rooms) // Quan trọng: Lấy thêm phòng của từng dãy
                .AsQueryable();

            if (propertyId.HasValue)
            {
                query = query.Where(p => p.Id == propertyId.Value);
            }

            ViewBag.PropertyList = _context.Properties.ToList();
            return View(query.ToList());
        }

        // GET: Create
        public IActionResult Create(Guid? propertyId)
        {
            ViewBag.Properties = _context.Properties.ToList();

            // Nếu có propertyId từ trang danh sách truyền sang thì gán, không thì để null
            var model = new Building();
            if (propertyId.HasValue && propertyId != Guid.Empty)
            {
                model.PropertyId = propertyId.Value;
            }

            return View(model);
        }

        [HttpPost]
        public IActionResult Create(Building building)
        {
            // LOG: Bạn có thể đặt breakpoint ở đây để kiểm tra ModelState
            if (building.PropertyId == Guid.Empty)
            {
                ModelState.AddModelError("PropertyId", "Vui lòng chọn một khu trọ hợp lệ.");
            }

            if (ModelState.IsValid)
            {
                building.Id = Guid.NewGuid();
                _context.Buildings.Add(building);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            // Nếu lỗi, phải load lại danh sách Properties cho Dropdown
            ViewBag.Properties = _context.Properties.ToList();
            return View(building);
        }

        // GET: Building/Edit/5
        public IActionResult Edit(Guid id)
        {
            var building = _context.Buildings.Find(id);
            if (building == null) return NotFound();

            // Giữ lại PropertyId để sau khi sửa xong quay về đúng khu trọ đó
            return View(building);
        }

        // POST: Building/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Guid id, Building building)
        {
            if (id != building.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var existingBuilding = _context.Buildings.Find(id);
                if (existingBuilding == null) return NotFound();

                existingBuilding.Name = building.Name; // Cập nhật tên mới
                _context.SaveChanges();

                return RedirectToAction("Index", "Building");
            }
            return View(building);
        }

        // POST: Building/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(Guid id)
        {
            var building = _context.Buildings
                .Include(b => b.Rooms)
                .FirstOrDefault(b => b.Id == id);

            if (building != null)
            {
                // Kiểm tra nếu dãy còn phòng thì không cho xóa (để an toàn dữ liệu)
                if (building.Rooms != null && building.Rooms.Any())
                {
                    TempData["Error"] = "Không thể xóa dãy đang có phòng. Vui lòng xóa hoặc chuyển phòng trước!";
                    return RedirectToAction("Index");
                }

                _context.Buildings.Remove(building);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}