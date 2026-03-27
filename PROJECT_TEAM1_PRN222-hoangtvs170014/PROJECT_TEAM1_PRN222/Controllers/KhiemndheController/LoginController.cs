using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BoardingHouseManagement.Models;

namespace PROJECT_TEAM1_PRN222.Controllers.KhiemndheController
{
    public class LoginController : Controller
    {
        private readonly AppDbContext _context;

        public LoginController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated) return RedirectToAction("Index", "Home");
            return View("~/Views/Khiemndhe/Login.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> Index(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.";
                return View("~/Views/Khiemndhe/Login.cshtml");
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                ViewBag.Error = "Tài khoản hoặc mật khẩu không chính xác.";
                return View("~/Views/Khiemndhe/Login.cshtml");
            }

            bool isPasswordCorrect = false;
            if (!user.PasswordHash.StartsWith("$2"))
            {
                if (user.PasswordHash == password)
                {
                    isPasswordCorrect = true;
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                isPasswordCorrect = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            }

            if (!isPasswordCorrect)
            {
                ViewBag.Error = "Tài khoản hoặc mật khẩu không chính xác.";
                return View("~/Views/Khiemndhe/Login.cshtml");
            }

            if (!user.IsActive)
            {
                ViewBag.Error = "Tài khoản của bạn đã bị khóa.";
                return View("~/Views/Khiemndhe/Login.cshtml");
            }

            await SignInUser(user);

            if (user.Role.RoleName.ToLower() == "admin")
            {
                return RedirectToAction("Index", "Portal", new { area = "" });
            }
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult ExternalLogin()
        {
            var properties = new AuthenticationProperties { RedirectUri = Url.Action("ExternalLoginCallback", "Login") };
            return Challenge(properties, Microsoft.AspNetCore.Authentication.Google.GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback()
        {
            var result = await HttpContext.AuthenticateAsync("ExternalCookie");
            if (!result.Succeeded)
            {
                ViewBag.Error = "Lỗi đăng nhập từ hệ thống từ Google.";
                return View("~/Views/Khiemndhe/Login.cshtml");
            }

            var email = result.Principal.FindFirstValue(ClaimTypes.Email);
            var name = result.Principal.FindFirstValue(ClaimTypes.Name);

            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Không thể lấy Email từ tài khoản Google của bạn.";
                return View("~/Views/Khiemndhe/Login.cshtml");
            }

            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName.ToLower() == "user");
                if (role == null) 
                {
                    role = new Role { Id = Guid.NewGuid(), RoleName = "User" };
                    _context.Roles.Add(role);
                }

                user = new User
                {
                    Id = Guid.NewGuid(),
                    Username = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()), // Hashed random key
                    FullName = name ?? "Google User",
                    Email = email,
                    Phone = "",
                    IdentityNumber = "",
                    RoleId = role.Id,
                    IsActive = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            if (!user.IsActive)
            {
                ViewBag.Error = "Tài khoản của bạn đã bị khóa.";
                return View("~/Views/Khiemndhe/Login.cshtml");
            }

            await HttpContext.SignOutAsync("ExternalCookie");
            await SignInUser(user);

            if (user.Role.RoleName.ToLower() == "admin")
            {
                return RedirectToAction("Index", "Portal", new { area = "" });
            }
            return RedirectToAction("Index", "Home");
        }

        private async Task SignInUser(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role.RoleName)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        }
    }
}
