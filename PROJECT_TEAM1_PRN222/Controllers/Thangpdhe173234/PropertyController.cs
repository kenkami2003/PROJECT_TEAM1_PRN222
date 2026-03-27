using BoardingHouseManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace PROJECT_TEAM1_PRN222.Controllers.Thangpdhe173234
{

    public class PropertyController : Controller
    {
        private readonly AppDbContext _context;

        public PropertyController(AppDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            var list = _context.Properties.ToList();
            return View(list);
        }
        public IActionResult Create()
        {
            return View(); // sẽ tự tìm đúng path
        }
        [HttpPost]
        public IActionResult Create(Property property)
        {
            if (!ModelState.IsValid)
            {
                return View(property);
            }

            property.Id = Guid.NewGuid();

            _context.Properties.Add(property);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        public IActionResult Edit(Guid id)
        {
            var property = _context.Properties.Find(id);
            if (property == null) return NotFound();

            return View(property);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // Nhận id từ URL và object từ Form
        public IActionResult Edit(Guid id, Property property)
        {
            // Kiểm tra xem ID trên URL có khớp với ID ẩn trong Form không
            if (id != property.Id)
            {
                return BadRequest(); // Trả về 400 nếu có dấu hiệu giả mạo ID
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(property);
                    _context.SaveChanges();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Properties.Any(e => e.Id == property.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }

            // Nếu có lỗi validation (ví dụ tên trống), quay lại View
            return View(property);
        }


        // GET: Xác nhận xóa (Hoặc có thể xóa trực tiếp bằng POST)
        public IActionResult Delete(Guid id)
        {
            var property = _context.Properties.Find(id);
            if (property == null) return NotFound();

            return View(property); // Trả về view xác nhận xóa
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(Guid id)
        {
            var property = _context.Properties.Find(id);
            if (property != null)
            {
                // Lưu ý: Nếu có Ràng buộc khóa ngoại (Buildings), 
                // bạn cần xử lý xóa các Building liên quan hoặc báo lỗi.
                _context.Properties.Remove(property);
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Property/EditPricing/Guid
        public IActionResult EditPricing(Guid id)
        {
            var property = _context.Properties.Find(id);
            if (property == null) return NotFound();

            return View(property);
        }

        // POST: Property/EditPricing
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditPricing(Property model)
        {
            var property = _context.Properties.Find(model.Id);
            if (property == null) return NotFound();

            // Cập nhật đơn giá
            property.PriceUnitElectricity = model.PriceUnitElectricity;
            property.PriceUnitWater = model.PriceUnitWater;
            property.ServiceFee = model.ServiceFee;
            _context.SaveChanges();

            // Sau khi lưu xong quay lại trang quản lý dãy
            return RedirectToAction("Index", "Building");
        }
    }
}
