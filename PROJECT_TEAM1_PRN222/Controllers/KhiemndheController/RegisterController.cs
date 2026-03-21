using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BoardingHouseManagement.Models;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace PROJECT_TEAM1_PRN222.Controllers.KhiemndheController
{
    public class RegisterController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _config;

        public RegisterController(AppDbContext context, IMemoryCache cache, IConfiguration config)
        {
            _context = context;
            _cache = cache;
            _config = config;
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

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrEmpty(email)) return false;
            var regex = @"^[a-zA-Z0-9_-]+(\.[a-zA-Z0-9_-]+)*@[a-zA-Z0-9-]+(\.[a-zA-Z0-9-]+)*\.[a-zA-Z]{2,}$";
            return Regex.IsMatch(email, regex);
        }

        [HttpGet]
        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated) return RedirectToAction("Index", "Home");
            return View("~/Views/Khiemndhe/Register.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> Index(string username, string password, string confirmpass, string fullname, string email)
        {
            ViewBag.Username = username;
            ViewBag.FullName = fullname;
            ViewBag.Email = email;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmpass) || string.IsNullOrEmpty(fullname) || string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ tất cả thông tin đăng ký.";
                return View("~/Views/Khiemndhe/Register.cshtml");
            }

            if (!IsValidEmail(email))
            {
                ViewBag.Error = "Địa chỉ email chưa đúng định dạng. (VD hợp lệ: name@domain.com, không được có dấu chấm kề nhau hoặc ở đầu/cuối tên).";
                return View("~/Views/Khiemndhe/Register.cshtml");
            }

            if (password != confirmpass)
            {
                ViewBag.Error = "Mật khẩu xác nhận không trùng khớp.";
                return View("~/Views/Khiemndhe/Register.cshtml");
            }

            if (!IsValidPassword(password))
            {
                ViewBag.Error = "Mật khẩu phải từ 8 ký tự và chứa ít nhất 3 trong 4 loại: chữ hoa, chữ thường, số, ký tự đặc biệt.";
                return View("~/Views/Khiemndhe/Register.cshtml");
            }

            if (await _context.Users.AnyAsync(u => u.Username == username))
            {
                ViewBag.Error = "Tên đăng nhập đã tồn tại trên hệ thống.";
                return View("~/Views/Khiemndhe/Register.cshtml");
            }

            if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                ViewBag.Error = "Địa chỉ Email này đã được sử dụng bởi một tài khoản khác.";
                return View("~/Views/Khiemndhe/Register.cshtml");
            }

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName.ToLower() == "user");
            if (role == null) 
            {
                role = new Role { Id = Guid.NewGuid(), RoleName = "User" };
                _context.Roles.Add(role);
            }

            var newUser = new User
            {
                Id = Guid.NewGuid(),
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                FullName = fullname,
                Email = email,
                Phone = "",
                IdentityNumber = "",
                RoleId = role.Id,
                IsActive = true
            };

            string otp = new Random().Next(100000, 999999).ToString();
            _cache.Set($"OTP_{email}", new Tuple<User, string>(newUser, otp), TimeSpan.FromMinutes(5));

            bool emailSent = await SendOtpEmailAsync(email, fullname, otp);
            if (!emailSent)
            {
                ViewBag.Error = "Hệ thống không thể gửi Email tới địa chỉ này. Vui lòng kiểm tra lại hòm thư của bạn.";
                return View("~/Views/Khiemndhe/Register.cshtml");
            }

            return RedirectToAction("VerifyEmail", new { email = email });
        }

        [HttpGet]
        public IActionResult VerifyEmail(string email)
        {
            if (string.IsNullOrEmpty(email) || !_cache.TryGetValue($"OTP_{email}", out _))
            {
                return RedirectToAction("Index", "Login");
            }
            ViewBag.Email = email;
            return View("~/Views/Khiemndhe/VerifyEmail.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> VerifyEmail(string email, string otp)
        {
            ViewBag.Email = email;

            if (_cache.TryGetValue($"OTP_{email}", out Tuple<User, string> cachedData))
            {
                if (cachedData.Item2 == otp)
                {
                    // OTP is valid, save to DB
                    _context.Users.Add(cachedData.Item1);
                    await _context.SaveChangesAsync();
                    
                    _cache.Remove($"OTP_{email}"); // Clean up

                    ViewBag.Success = "Xác thực Email thành công! Tài khoản đã được kích hoạt, vui lòng đăng nhập.";
                    return View("~/Views/Khiemndhe/Login.cshtml");
                }
                else
                {
                    ViewBag.Error = "Mã xác nhận (OTP) không chính xác. Vui lòng kiểm tra kỹ email.";
                    return View("~/Views/Khiemndhe/VerifyEmail.cshtml");
                }
            }

            ViewBag.Error = "Mã OTP đã hết hạn (quá 5 phút) hoặc không hợp lệ. Vui lòng đăng ký lại.";
            return RedirectToAction("Index", "Login");
        }

        private async Task<bool> SendOtpEmailAsync(string toEmail, string fullName, string otpCode)
        {
            try
            {
                var smtpSettings = _config.GetSection("SmtpSettings");
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(smtpSettings["SenderEmail"], smtpSettings["SenderName"]),
                    Subject = "[Trọ FPT] Mã Xác Nhận Đăng Ký Tài Khoản",
                    Body = $@"
                        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; border: 1px solid #ddd; padding: 20px; border-radius: 10px;'>
                            <h2 style='color: #00B98E; text-align: center;'>Chào mừng bạn gia nhập!</h2>
                            <p>Xin chào <strong>{fullName}</strong>,</p>
                            <p>Đây là mã xác nhận (OTP) để kích hoạt tài khoản hệ thống của bạn. Quý khách vui lòng không chia sẻ mã này cho bất kỳ ai.</p>
                            <div style='text-align: center; margin: 30px 0;'>
                                <span style='font-size: 32px; font-weight: bold; background: #f4f4f4; padding: 10px 20px; border-radius: 5px; color: #333; letter-spacing: 5px;'>{otpCode}</span>
                            </div>
                            <p style='color: #ff4a4a; font-size: 13px;'>*Lưu ý: Mã OTP này có hiệu lực trong vòng 5 phút.</p>
                            <hr style='border-top: 1px solid #eee; margin-top: 40px;'/>
                            <p style='font-size: 12px; color: #888; text-align: center;'>Hệ thống Quản Lý Trọ FPT</p>
                        </div>
                    ",
                    IsBodyHtml = true,
                };
                mailMessage.To.Add(toEmail);

                using var smtpClient = new SmtpClient(smtpSettings["Server"], int.Parse(smtpSettings["Port"]))
                {
                    Credentials = new NetworkCredential(smtpSettings["Username"], smtpSettings["Password"]),
                    EnableSsl = true
                };

                await smtpClient.SendMailAsync(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi gửi Email (OTP): " + ex.Message);
                return false;
            }
        }
    }
}
