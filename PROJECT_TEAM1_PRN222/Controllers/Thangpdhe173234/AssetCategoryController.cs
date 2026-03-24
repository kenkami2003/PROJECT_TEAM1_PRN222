using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BoardingHouseManagement.Models;

namespace BoardingHouseManagement.Controllers
{
    public class AssetCategoryController : Controller
    {
        private readonly AppDbContext _context;

        public AssetCategoryController(AppDbContext context)
        {
            _context = context;
        }

        // GET: AssetCategory (Danh sách các loại tài sản)
        public async Task<IActionResult> Index()
        {
            // Lấy danh sách tất cả loại tài sản đã tạo
            var categories = await _context.AssetCategories.ToListAsync();
            return View(categories);
        }

        // GET: AssetCategory/Create (Trang tạo mới)
        public IActionResult Create()
        {
            return View();
        }

        // POST: AssetCategory/Create (Xử lý lưu vào DB)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AssetCategory category)
        {
            if (ModelState.IsValid)
            {
                category.Id = Guid.NewGuid();
                _context.Add(category);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(category);


        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var category = await _context.AssetCategories.FindAsync(id);

            if (category == null) return NotFound();

            // Kiểm tra xem có phòng nào đang sử dụng loại tài sản này không (Ràng buộc dữ liệu)
            var isUsed = _context.RoomAssets.Any(a => a.AssetCategoryId == id);
            if (isUsed)
            {
                // Nếu đang dùng thì không cho xóa, báo lỗi hoặc quay về trang Index
                TempData["Error"] = "Không thể xóa vì có phòng đang sử dụng loại tài sản này!";
                return RedirectToAction(nameof(Index));
            }

            _context.AssetCategories.Remove(category);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: AssetCategory/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();
            var category = await _context.AssetCategories.FindAsync(id);
            if (category == null) return NotFound();
            return View(category);
        }

        // POST: AssetCategory/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, AssetCategory category)
        {
            if (id != category.Id) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(category);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }


    }
}