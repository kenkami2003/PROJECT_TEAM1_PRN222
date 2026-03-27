using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BoardingHouseManagement.Models;
using System.Text.RegularExpressions;

namespace PROJECT_TEAM1_PRN222.Controllers.KhiemndheController
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly AppDbContext _context;

        public ProfileController(AppDbContext context)
        {
            _context = context;
        }

        private bool IsValidPhoneNumber(string phone)
        {
            if (string.IsNullOrEmpty(phone)) return false;
            var regex = @"^(0|84|\+84)[3|5|7|8|9][0-9]{8}$";
            return Regex.IsMatch(phone, regex);
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return RedirectToAction("Logout");

            var guardian = await _context.Guardians.FirstOrDefaultAsync(g => g.TenantId == userId);
            
            ViewBag.Guardian = guardian;
            return View("~/Views/Khiemndhe/Profile.cshtml", user);
        }

        [HttpPost]
        public async Task<IActionResult> Index(string FullName, string Email, string Phone, string IdentityNumber, string guardianName, string guardianPhone, string guardianRelationship)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var guardian = await _context.Guardians.FirstOrDefaultAsync(g => g.TenantId == userId);

            if (!string.IsNullOrEmpty(Phone) && !IsValidPhoneNumber(Phone))
            {
                ViewBag.Error = "Số điện thoại cá nhân không hợp lệ (phải bắt đầu bằng 03, 05, 07, 08, 09 và đủ 10 số).";
                ViewBag.Guardian = guardian;
                return View("~/Views/Khiemndhe/Profile.cshtml", user);
            }

            if (!string.IsNullOrEmpty(guardianPhone) && !IsValidPhoneNumber(guardianPhone))
            {
                ViewBag.Error = "Số điện thoại người bảo hộ không hợp lệ (phải bắt đầu bằng 03, 05, 07, 08, 09 và đủ 10 số).";
                ViewBag.Guardian = guardian;
                return View("~/Views/Khiemndhe/Profile.cshtml", user);
            }

            if (user != null)
            {
                user.FullName = string.IsNullOrEmpty(FullName) ? user.FullName : FullName;
                user.Email = string.IsNullOrEmpty(Email) ? user.Email : Email;
                user.Phone = string.IsNullOrEmpty(Phone) ? user.Phone : Phone;
                user.IdentityNumber = string.IsNullOrEmpty(IdentityNumber) ? user.IdentityNumber : IdentityNumber;
            }

            if (guardian == null)
            {
                if (!string.IsNullOrEmpty(guardianName))
                {
                    guardian = new Guardian
                    {
                        Id = Guid.NewGuid(),
                        TenantId = userId,
                        Name = guardianName,
                        Phone = guardianPhone,
                        Relationship = guardianRelationship
                    };
                    _context.Guardians.Add(guardian);
                }
            }
            else
            {
                guardian.Name = string.IsNullOrEmpty(guardianName) ? guardian.Name : guardianName;
                guardian.Phone = string.IsNullOrEmpty(guardianPhone) ? guardian.Phone : guardianPhone;
                guardian.Relationship = string.IsNullOrEmpty(guardianRelationship) ? guardian.Relationship : guardianRelationship;
            }

            await _context.SaveChangesAsync();
            ViewBag.Success = "Cập nhật hồ sơ thành công!";
            ViewBag.Guardian = guardian;

            return View("~/Views/Khiemndhe/Profile.cshtml", user);
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Users", new { area = "Datmthe176706" });
        }
    }
}
