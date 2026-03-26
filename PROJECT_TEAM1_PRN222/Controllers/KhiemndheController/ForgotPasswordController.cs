using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BoardingHouseManagement.Models;

namespace PROJECT_TEAM1_PRN222.Controllers.KhiemndheController
{
    public class ForgotPasswordController : Controller
    {
        private readonly AppDbContext _context;

        public ForgotPasswordController(AppDbContext context)
        {
            _context = context;
        }

        private bool IsValidPassword(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8) return false;

            int criteriaMet = 0;
            if (password.Any(char.IsUpper)) criteriaMet++;
            if (password.Any(char.IsLower)) criteriaMet++;
            if (password.Any(char.IsDigit)) criteriaMet++;
            if (password.Any(c => !char.IsLetterOrDigit(c))) criteriaMet++;

            return criteriaMet >= 3;
        }

        [HttpGet]
        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated) return RedirectToAction("Index", "Home");
            return View("~/Views/Khiemndhe/ForgotPassword.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> Index(string username, string email, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                ViewBag.Error = "Vui lòng điền đầy đủ thông tự.";
                return View("~/Views/Khiemndhe/ForgotPassword.cshtml");
            }
            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp.";
                return View("~/Views/Khiemndhe/ForgotPassword.cshtml");
            }

            if (!IsValidPassword(newPassword))
            {
                ViewBag.Error = "Mật khẩu mới phải từ 8 ký tự và chứa ít nhất 3 trong 4 loại: chữ hoa, chữ thường, số, ký tự đặc biệt.";
                return View("~/Views/Khiemndhe/ForgotPassword.cshtml");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username && u.Email == email);
            if (user == null)
            {
                ViewBag.Error = "Tên đăng nhập và Email không hợp lệ.";
                return View("~/Views/Khiemndhe/ForgotPassword.cshtml");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _context.SaveChangesAsync();

            ViewBag.Success = "Đổi mật khẩu thành công! Vui lòng đăng nhập lại.";
            return View("~/Views/Khiemndhe/Login.cshtml");
        }
    }
}
