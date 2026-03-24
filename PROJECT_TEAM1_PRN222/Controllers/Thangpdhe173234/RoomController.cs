using BoardingHouseManagement.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace PROJECT_TEAM1_PRN222.Controllers.Thangpdhe173234
{
    public class RoomController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;
        public async Task<IActionResult> Index()
        {
            var rooms = await _context.Rooms
                .Include(r => r.Building)
                .ToListAsync();

            return View(rooms);
        }
        public RoomController(AppDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
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
            // Cần thêm .Include để load danh sách tài sản kèm theo phòng
            var room = _context.Rooms
                .Include(r => r.Building)
                .Include(r => r.RoomAssets) 
                    .ThenInclude(a => a.AssetCategory) // Load luôn tên loại (Tủ lạnh, Điều hòa)
                .FirstOrDefault(r => r.Id == id);

            if (room == null) return NotFound();

            // Các phần xử lý SelectList giữ nguyên...
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
        public async Task<IActionResult> Edit(Guid id, Room room)
        {
            if (id != room.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // 1. Cập nhật thông tin phòng trước
                    _context.Update(room);

                    // 2. Xử lý danh sách thiết bị đi kèm (RoomAssets)
                    // Lấy lại danh sách thiết bị hiện tại của phòng này từ DB
                    var currentAssets = _context.RoomAssets.Where(a => a.RoomId == id).ToList();

                    foreach (var asset in currentAssets)
                    {
                        // Đọc Tình trạng mới từ View (Khớp với name="Condition_@asset.Id")
                        string conditionKey = "Condition_" + asset.Id;
                        if (Request.Form.ContainsKey(conditionKey))
                        {
                            asset.Condition = Request.Form[conditionKey];
                        }

                        // Đọc File ảnh mới (Khớp với name="AssetFiles_@asset.Id")
                        var file = Request.Form.Files.GetFile("AssetFiles_" + asset.Id);
                        if (file != null && file.Length > 0)
                        {
                            // Tạo thư mục nếu chưa có
                            string folder = Path.Combine(_hostEnvironment.WebRootPath, "uploads/assets");
                            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                            // Lưu file
                            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                            string filePath = Path.Combine(folder, fileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            // Cập nhật đường dẫn ảnh vào database
                            asset.ImageUrl = "/uploads/assets/" + fileName;
                        }
                    }

                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    // Xử lý lỗi nếu cần
                }
            }

            // Nếu có lỗi thì phải load lại các danh sách cho Dropdown
            ViewBag.BuildingId = new SelectList(_context.Buildings, "Id", "Name", room.BuildingId);
            return View(room);
        }

        public IActionResult Details(Guid id)
        {
            var room = _context.Rooms
                .Include(r => r.Building)
                .Include(r => r.RoomAssets)
                    .ThenInclude(a => a.AssetCategory)
                .FirstOrDefault(r => r.Id == id);

            if (room == null) return NotFound();
            ViewBag.AssetCategories = _context.AssetCategories.ToList();
            return View(room);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAsset(RoomAsset asset, IFormFile? AssetImage)
        {
            try
            {
                // 1. Gán ID mới cho bản ghi tài sản
                asset.Id = Guid.NewGuid();

                // 2. Xử lý lưu ảnh nếu người dùng có chụp ảnh/chọn file ngay lúc thêm
                if (AssetImage != null && AssetImage.Length > 0)
                {
                    string wwwRootPath = _hostEnvironment.WebRootPath;
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(AssetImage.FileName);
                    string path = Path.Combine(wwwRootPath, "uploads", "assets");

                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                    using (var fileStream = new FileStream(Path.Combine(path, fileName), FileMode.Create))
                    {
                        await AssetImage.CopyToAsync(fileStream);
                    }
                    asset.ImageUrl = "/uploads/assets/" + fileName;
                }

                // 3. Lưu vào Database
                _context.RoomAssets.Add(asset);
                await _context.SaveChangesAsync();

                // 4. Quay lại trang Details của chính phòng đó
                return RedirectToAction("Details", new { id = asset.RoomId });
            }
            catch (Exception ex)
            {
                // Nếu lỗi, quay lại trang Index hoặc báo lỗi
                return BadRequest("Lỗi khi thêm tài sản: " + ex.Message);
            }
        }

    }
}
