using BoardingHouseManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace PROJECT_TEAM1_PRN222.Controllers.Thangpdhe173234
{
    public class RoomController : Controller
    {
        private readonly AppDbContext _context;
        public IActionResult Index()
        {
            return View();
        }
        public RoomController(AppDbContext context)
        {
            _context = context;
        }


        // 1. Hàm này để HIỂN THỊ cái Form (Khi bạn gõ URL hoặc bấm nút)
        [HttpGet] // Phải có cái này hoặc để trống (mặc định là GET)
        public IActionResult Create(Guid buildingId)
        {
            var building = _context.Buildings
                .Include(b => b.Property)
                .FirstOrDefault(b => b.Id == buildingId);

            if (building == null) return NotFound();

            ViewBag.BuildingName = building.Name;
            ViewBag.PropertyName = building.Property?.Name;

            return View(new Room { BuildingId = buildingId });
        }

        // 2. Hàm này để XỬ LÝ khi bấm nút "Lưu" (Submit Form)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Room room)
        {
            // 1. Kiểm tra xem số phòng này đã tồn tại trong Dãy này chưa
            bool isExist = _context.Rooms.Any(r => r.BuildingId == room.BuildingId && r.RoomNumber == room.RoomNumber);

            if (isExist)
            {
                // Thêm lỗi thủ công vào ModelState để hiển thị ra View
                ModelState.AddModelError("RoomNumber", "Số phòng này đã tồn tại trong dãy này rồi!");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    room.Id = Guid.NewGuid();
                    _context.Rooms.Add(room);
                    _context.SaveChanges();
                    return RedirectToAction("Index", "Building");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra khi lưu: " + ex.Message);
                }
            }

            // Nếu có lỗi (trùng số phòng hoặc validate thất bại), nạp lại dữ liệu cho View
            var building = _context.Buildings.Include(b => b.Property).FirstOrDefault(b => b.Id == room.BuildingId);
            ViewBag.BuildingName = building?.Name;
            ViewBag.PropertyName = building?.Property?.Name;

            return View(room);
        }

        public IActionResult Edit(Guid id)
        {
            var room = _context.Rooms.Include(r => r.Building).FirstOrDefault(r => r.Id == id);
            if (room == null) return NotFound();

            // Lấy danh sách dãy trọ thuộc cùng một khu trọ để hiển thị trong Dropdown
            var currentPropertyId = _context.Buildings
                .Where(b => b.Id == room.BuildingId)
                .Select(b => b.PropertyId)
                .FirstOrDefault();

            ViewBag.BuildingId = new SelectList(
                _context.Buildings.Where(b => b.PropertyId == currentPropertyId),
                "Id", "Name", room.BuildingId);

            return View(room);
        }

        // POST: Room/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Guid id, Room room)
        {
            if (id != room.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(room);
                    _context.SaveChanges();
                    return RedirectToAction("Index", "Building"); // Quay lại trang quản lý chính
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi khi cập nhật: " + ex.Message);
                }
            }
            return View(room);
        }

    }
}
