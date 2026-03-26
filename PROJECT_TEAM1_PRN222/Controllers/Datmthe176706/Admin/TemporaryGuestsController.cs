using BoardingHouseManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PROJECT_TEAM1_PRN222.Controllers.Datmthe176706.Admin
{
    [Area("Datmthe176706")]
    public class TemporaryGuestsController : Controller
    {
        private readonly AppDbContext _context;

        public TemporaryGuestsController(AppDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }

        // List
        public async Task<IActionResult> TemporaryGuestList(string status, Guid? buildingId, string searchTerm, DateTime? expiryDate)
        {
            ViewBag.Buildings = await _context.Buildings.ToListAsync();
            ViewBag.SelectedBuilding = buildingId;

            var query = _context.TemporaryGuests
                .Include(t => t.Room)
                    .ThenInclude(r => r.Building)
                .AsQueryable();

            if (buildingId.HasValue)
            {
                query = query.Where(t => t.Room.BuildingId == buildingId);
            }
            // tìm theo trạng thái
            if (!string.IsNullOrEmpty(status))
            {
                switch (status)
                {
                    case "Pending": // Chưa thanh toán
                        query = query.Where(t => !t.IsPaid);
                        break;
                    case "Active": // Đang ở
                        query = query.Where(t => !t.IsCheckedOut && t.ArrivalDate <= DateTime.Now && t.DepartureDate >= DateTime.Now);
                        break;
                    case "CheckedOut": // Đã trả phòng
                        query = query.Where(t => t.IsCheckedOut);
                        break;
                }
            }
            // tìm theo tên hoặc số phòng
            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(t => t.FullName.ToLower().Contains(searchTerm)
                                      || t.Room.RoomNumber.ToLower().Contains(searchTerm));
            }
            // tìm theo ngày kết thúc
            if (expiryDate.HasValue)
            {
                query = query.Where(t => t.DepartureDate.Date == expiryDate.Value.Date);
            }

            var result = await query.OrderByDescending(t => t.ArrivalDate).ToListAsync();

            // Lưu lại giá trị để hiển thị ngược lên Form sau khi Load trang
            ViewBag.CurrentSearch = searchTerm;
            ViewBag.CurrentExpiryDate = expiryDate?.ToString("yyyy-MM-dd");
            ViewBag.CurrentStatus = status;

            return View("~/Views/Datmthe176706/Admin/TemporaryGuestList.cshtml", result);
        }

        // Detail
        public async Task<IActionResult> TemporaryDetails(Guid id)
        {
            var guest = await _context.TemporaryGuests
                .Include(t => t.Room)
                .ThenInclude(r => r.Building)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (guest == null) return NotFound();
            return View("~/Views/Datmthe176706/Admin/TemporaryDetails.cshtml", guest);
        }

        // Create
        [HttpGet]
        public async Task<IActionResult> CreateShortTerm()
        {
            ViewBag.Rooms = await _context.Rooms
                .Where(r => r.Status == RoomStatus.Available)
                .ToListAsync();

            return View("~/Views/Datmthe176706/Admin/CreateShortTerm.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateShortTerm(TemporaryGuest guest, IFormFile frontImg, IFormFile backImg)
        {
            // Loại bỏ kiểm tra validate cho các trường ảnh (vì mình xử lý thủ công bằng IFormFile)
            ModelState.Remove("IdentityCardFront");
            ModelState.Remove("IdentityCardBack");
            ModelState.Remove("PaymentProof");
            ModelState.Remove("Room"); // Xóa bỏ validate object Room liên quan

            if (ModelState.IsValid)
            {
                if (frontImg != null) guest.IdentityCardFront = await SaveImage(frontImg);
                if (backImg != null) guest.IdentityCardBack = await SaveImage(backImg);

                guest.Id = Guid.NewGuid();
                guest.IsPaid = false;
                guest.IsCheckedOut = false;

                var room = await _context.Rooms.FindAsync(guest.RoomId);
                if (room != null)
                {
                    room.Status = RoomStatus.Reserved;

                    int totalDays = (guest.DepartureDate - guest.ArrivalDate).Days;
                    if (totalDays <= 0) totalDays = 1;
                    guest.TotalAmount = ((room.BasePrice / 30) + 25000) * totalDays;
                }

                _context.TemporaryGuests.Add(guest);
                await _context.SaveChangesAsync();


                return RedirectToAction("PaymentQR", new { id = guest.Id });
            }

            // Nếu lỗi Validation, load lại danh sách phòng và trả về View Form
            ViewBag.Rooms = await _context.Rooms.Where(r => r.Status == RoomStatus.Available).ToListAsync();
            return View("~/Views/Datmthe176706/Admin/CreateShortTerm.cshtml", guest);
        }

        // Hàm hỗ trợ lưu ảnh
        private async Task<string> SaveImage(IFormFile file)
        {
            string fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", fileName);
            using (var stream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return fileName;
        }

        // thanh toán, biên lai
        public async Task<IActionResult> PaymentQR(Guid id)
        {
            var guest = await _context.TemporaryGuests
                .Include(t => t.Room)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (guest == null) return NotFound();

            // Thông tin ngân hàng 
            string bankId = "BIDV"; 
            string accountNo = "5011801614";
            string accountName = "MAI TIEN DAT";
            string description = $"Thanh toan phong {guest.Room?.RoomNumber}";

            // Tạo link VietQR tự động
            ViewBag.QrUrl = $"https://img.vietqr.io/image/{bankId}-{accountNo}-compact2.png?amount={guest.TotalAmount}&addInfo={description}&accountName={accountName}";

            return View("~/Views/Datmthe176706/Admin/PaymentQrTeamporary.cshtml", guest);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmPayment(Guid id, IFormFile paymentProof)
        {
            var guest = await _context.TemporaryGuests.FindAsync(id);
            if (guest == null) return NotFound();

            if (paymentProof != null)
            {
                guest.PaymentProof = await SaveImage(paymentProof);
                guest.IsPaid = true;

                var room = await _context.Rooms.FindAsync(guest.RoomId);
                if (room != null) room.Status = RoomStatus.Occupied;

                _context.Update(guest);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Xác nhận thanh toán thành công!";
                return RedirectToAction("TemporaryGuestList");
            }

            ModelState.AddModelError("", "Vui lòng tải ảnh biên lai xác nhận.");
            return RedirectToAction("~/Views/Datmthe176706/Admin/TemporaryGuestList.cshtml", new { id = id });
        }

        // Trả phòng thủ công
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOut(Guid id)
        {
            var guest = await _context.TemporaryGuests.FindAsync(id);
            if (guest == null) return Json(new { success = false, message = "Không tìm thấy khách." });

            guest.IsCheckedOut = true;

            var room = await _context.Rooms.FindAsync(guest.RoomId);
            if (room != null)
            {
                room.Status = RoomStatus.Available; 
                _context.Update(room);
            }

            _context.Update(guest);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã trả phòng và giải phóng phòng trống!" });
        }
    }
}

