using BoardingHouseManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROJECT_TEAM1_PRN222.Services;

namespace PROJECT_TEAM1_PRN222.Controllers.Datmthe176706.Users
{
    [Area("Datmthe176706")]
    public class UsersController : Controller
    {
        private readonly AppDbContext _context;
        public UsersController(AppDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {

            var model = _context.Properties
                   .Include(p => p.Buildings)
                       .ThenInclude(b => b.Rooms)
                   .ToList();

            return View("~/Views/Datmthe176706/Users/Index.cshtml", model);
        }


        //Room detail test
        public async Task<IActionResult> Details(Guid id)
        {
            var room = await _context.Rooms
                .Include(r => r.Building)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (room == null) return NotFound();

            // --- PHẦN CỦA BẠN (PHẢI MỞ COMMENT RA) ---
            Guid testUserId = Guid.Parse("C0940C8B-5BEA-4D67-869A-252045133C1E");

            // Gọi helper để check
            ViewBag.IsMyReservation = (bool)await ReservationHelper.IsUserReservedRoom(_context, id, testUserId);
            ViewBag.CurrentUserId = testUserId;
            // ----------------------------------------

            return View("~/Views/Datmthe176706/Users/DetailRoomTest.cshtml", room);
        }


        // Reservation

        // Đặt phòng
        // [Authorize] // Tạm comment để test không cần login
        [HttpGet]
        public async Task<IActionResult> Reservation(Guid roomId)
        {
            // --- PHẦN SESSION (TẠM COMMENT) ---
            // var userIdStr = HttpContext.Session.GetString("UserId");
            // if (string.IsNullOrEmpty(userIdStr))
            // {
            //      return RedirectToAction("Login", "Account", new { area = "Datmthe176706" });
            // }
            // var userId = Guid.Parse(userIdStr);
            // ----------------------------------

            // --- ID GIẢ ĐỂ TEST (Thay bằng 1 ID có thật trong bảng User của bạn) ---
            Guid userId = Guid.Parse("C0940C8B-5BEA-4D67-869A-252045133C1E");
            // ----------------------------------------------------------------------

            var user = await _context.Users.FindAsync(userId);

            var room = await _context.Rooms
                .Include(r => r.Building)
                .FirstOrDefaultAsync(m => m.Id == roomId);

            if (room == null) return NotFound();

            ViewBag.CurrentUser = user;
            return View("~/Views/Datmthe176706/Users/Reservation.cshtml", room);
        }

        // [Authorize] // Tạm comment để test
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Thêm tham số IFormFile proofFile để nhận ảnh từ Form
        public async Task<IActionResult> CreateReservation(Reservation reservation, IFormFile? proofFile)
        {
            // --- PHẦN SESSION (TẠM COMMENT) ---
            // var userIdStr = HttpContext.Session.GetString("UserId");
            // if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            // reservation.GuestId = Guid.Parse(userIdStr);
            // ----------------------------------

            // --- ID GIẢ ĐỂ TEST (Trùng với ID ở hàm GET bên trên) ---
            reservation.GuestId = Guid.Parse("C0940C8B-5BEA-4D67-869A-252045133C1E");
            // -------------------------------------------------------

            if (reservation.RoomId == Guid.Empty)
            {
                TempData["Error"] = "Thông tin phòng không hợp lệ.";
                return RedirectToAction("Index");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var room = await _context.Rooms.FindAsync(reservation.RoomId);

                if (room == null)
                {
                    TempData["Error"] = "Phòng không tồn tại.";
                    return RedirectToAction("Index");
                }

                if ((int)room.Status != 0)
                {
                    TempData["Error"] = "Phòng không khả dụng.";
                    return RedirectToAction("Index");
                }

                // --- XỬ LÝ LƯU FILE ẢNH MINH CHỨNG ---
                if (proofFile != null && proofFile.Length > 0)
                {
                    // Tạo đường dẫn lưu file: wwwroot/uploads/proofs/
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "proofs");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    // Tạo tên file duy nhất để tránh trùng lặp
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(proofFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await proofFile.CopyToAsync(fileStream);
                    }

                    // Lưu tên file vào cột PaymentProofImage (hoặc cột tương ứng trong Model của bạn)
                    reservation.PaymentProofImage = uniqueFileName;
                }
                // ------------------------------------

                reservation.Id = Guid.NewGuid();
                reservation.CreatedAt = DateTime.Now;
                reservation.ReservedDate = DateTime.Now;
                reservation.ExpiryDate = DateTime.Now.AddDays(2);
                reservation.IsConvertedToContract = false;
                reservation.Status = ReservationStatus.Pending;

                // Phí giữ chỗ tính toán lại từ Room (đề phòng user can thiệp input ẩn)
                reservation.ReservationFee = room.DepositAmount / 2;

                // Cập nhật trạng thái phòng sang "Đã giữ chỗ" (thường là status 2)
                room.Status = (RoomStatus)2;

                _context.Reservations.Add(reservation);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = "Đã gửi yêu cầu giữ chỗ và minh chứng thành công. Vui lòng chờ Admin phê duyệt!";

                // Không đi tới PaymentGuidance nữa, quay về trang danh sách phòng hoặc trang cá nhân
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // 1. Trang hướng dẫn thanh toán
        public async Task<IActionResult> PaymentGuidance(Guid id)
        {
            var reservation = await _context.Reservations
                .Include(r => r.Room)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (reservation == null) return NotFound();

            return View("~/Views/Datmthe176706/Users/PaymentGuidance.cshtml", reservation);
        }





        [HttpGet]
        public async Task<IActionResult> CreateContract(Guid roomId)
        {
            Guid testUserId = Guid.Parse("C0940C8B-5BEA-4D67-869A-252045133C1E");

            var room = await _context.Rooms
                .Include(r => r.Building)
                .Include(r => r.RoomAssets).ThenInclude(ra => ra.AssetCategory)
                .FirstOrDefaultAsync(m => m.Id == roomId);

            if (room == null) return NotFound();

            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.RoomId == roomId &&
                                          r.GuestId == testUserId &&
                                          r.Status == ReservationStatus.Confirmed &&
                                          !r.IsConvertedToContract);

            decimal actualDeposit = room.DepositAmount;
            bool isFromReservation = false;

            if (reservation != null)
            {
                actualDeposit = room.DepositAmount - reservation.ReservationFee;
                isFromReservation = true;
            }

            ViewBag.CurrentUser = await _context.Users.FindAsync(testUserId);
            ViewBag.ActualDeposit = actualDeposit;
            ViewBag.IsFromReservation = isFromReservation;

            ViewBag.ServiceFees = await _context.ServiceFees.ToListAsync();

            return View("~/Views/Datmthe176706/Users/CreateContract.cshtml", room);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitContract(Guid RoomId, Guid TenantId, decimal ActualDeposit, IFormFile ContractProof, DateTime StartDate)
        {
            if (ContractProof == null || ContractProof.Length == 0)
            {
                TempData["Error"] = "Vui lòng tải lên ảnh minh chứng.";
                return RedirectToAction("CreateContract", new { roomId = RoomId });
            }

            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(ContractProof.FileName);
            string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "contracts");
            if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

            using (var stream = new FileStream(Path.Combine(uploadPath, fileName), FileMode.Create))
            {
                await ContractProof.CopyToAsync(stream);
            }

            var contract = new Contract
            {
                Id = Guid.NewGuid(),
                // Sinh mã hợp đồng tự động để thỏa mãn [Required] ContractCode
                ContractCode = "HD-" + DateTime.Now.ToString("yyyyMMdd") + "-" + Guid.NewGuid().ToString().Substring(0, 4).ToUpper(),
                RoomId = RoomId,
                TenantId = TenantId,
                StartDate = StartDate,
                EndDate = StartDate.AddMonths(12),
                ActualDeposit = ActualDeposit,
                IsActive = false, // Chờ duyệt
                ContractProofImage = fileName,
                CreatedAt = DateTime.Now
            };

            _context.Contracts.Add(contract);

            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.RoomId == RoomId && r.GuestId == TenantId && !r.IsConvertedToContract);
            if (reservation != null) reservation.IsConvertedToContract = true;

            var room = await _context.Rooms.FindAsync(RoomId);
            if (room != null) room.Status = RoomStatus.Reserved;

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}
