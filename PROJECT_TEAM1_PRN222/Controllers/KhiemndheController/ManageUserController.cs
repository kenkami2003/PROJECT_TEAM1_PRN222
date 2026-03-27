using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BoardingHouseManagement.Models;
using System.Text.RegularExpressions;

namespace PROJECT_TEAM1_PRN222.Controllers.KhiemndheController
{
    [Authorize]
    [Route("Admin/User")]
    public class ManageUserController : Controller
    {
        private readonly AppDbContext _context;

        public ManageUserController(AppDbContext context)
        {
            _context = context;
        }

        private bool IsValidPhoneNumber(string phone)
        {
            if (string.IsNullOrEmpty(phone)) return false;
            var regex = @"^(0|84|\+84)[3|5|7|8|9][0-9]{8}$";
            return Regex.IsMatch(phone, regex);
        }

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(string searchString)
        {
            var currentUserRole = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            if (currentUserRole?.ToLower() != "admin") return RedirectToAction("Index", "Home");

            var usersQuery = _context.Users.Include(u => u.Role).AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                var lowerSearch = searchString.ToLower();
                usersQuery = usersQuery.Where(u => 
                    (u.FullName != null && u.FullName.ToLower().Contains(lowerSearch)) ||
                    (u.Username != null && u.Username.ToLower().Contains(lowerSearch)) ||
                    (u.Email != null && u.Email.ToLower().Contains(lowerSearch)) ||
                    (u.Phone != null && u.Phone.Contains(searchString))
                );
            }

            var users = await usersQuery.ToListAsync();
            var guardians = await _context.Guardians.ToDictionaryAsync(g => g.TenantId);
            
            var activeContracts = await _context.Contracts
                .Include(c => c.Room)
                .ThenInclude(r => r.Building)
                .Where(c => c.IsActive)
                .ToDictionaryAsync(c => c.TenantId);

            ViewBag.Guardians = guardians;
            ViewBag.ActiveContracts = activeContracts;
            ViewBag.SearchString = searchString;

            return View("~/Views/Khiemndhe/ListUser.cshtml", users);
        }

        [HttpGet("Create")]
        public async Task<IActionResult> Create()
        {
            var currentUserRole = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            if (currentUserRole?.ToLower() != "admin") return RedirectToAction("Index", "Home");

            ViewBag.Roles = await _context.Roles.ToListAsync();
            return View("~/Views/Khiemndhe/CreateUser.cshtml");
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create(string Username, string Password, string FullName, string Email, string Phone, string IdentityNumber, Guid RoleId, bool IsActive)
        {
            var currentUserRole = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            if (currentUserRole?.ToLower() != "admin") return RedirectToAction("Index", "Home");

            ViewBag.Roles = await _context.Roles.ToListAsync();

            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            {
                ViewBag.Error = "Tên đăng nhập và mật khẩu là bắt buộc.";
                return View("~/Views/Khiemndhe/CreateUser.cshtml");
            }

            if (await _context.Users.AnyAsync(u => u.Username == Username))
            {
                ViewBag.Error = "Tên đăng nhập đã tồn tại.";
                return View("~/Views/Khiemndhe/CreateUser.cshtml");
            }

            if (!string.IsNullOrEmpty(Phone) && !IsValidPhoneNumber(Phone))
            {
                ViewBag.Error = "Số điện thoại cá nhân không hợp lệ (phải bắt đầu bằng 03, 05, 07, 08, 09 và đủ 10 số).";
                return View("~/Views/Khiemndhe/CreateUser.cshtml");
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password),
                FullName = FullName ?? "",
                Email = Email ?? "",
                Phone = Phone ?? "",
                IdentityNumber = IdentityNumber ?? "",
                RoleId = RoleId,
                IsActive = IsActive
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var currentUserRole = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            if (currentUserRole?.ToLower() != "admin") return RedirectToAction("Index", "Home");

            var user = await _context.Users.Include(u=>u.Role).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            var guardian = await _context.Guardians.FirstOrDefaultAsync(g => g.TenantId == id);
            
            var activeContract = await _context.Contracts
                .Include(c => c.Room)
                .ThenInclude(r => r.Building)
                .Include(c => c.Room.RoomAssets)
                .ThenInclude(a => a.AssetCategory)
                .FirstOrDefaultAsync(c => c.TenantId == id && c.IsActive);

            ViewBag.Guardian = guardian;
            ViewBag.Roles = await _context.Roles.ToListAsync();
            ViewBag.ActiveContract = activeContract;

            return View("~/Views/Khiemndhe/EditUser.cshtml", user);
        }

        [HttpPost("Edit/{id}")]
        public async Task<IActionResult> Edit(Guid id, string FullName, string Email, string Phone, string IdentityNumber, bool IsActive, Guid RoleId, string guardianName, string guardianPhone, string guardianRelationship)
        {
            var currentUserRole = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            if (currentUserRole?.ToLower() != "admin") return RedirectToAction("Index", "Home");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            if (!string.IsNullOrEmpty(Phone) && !IsValidPhoneNumber(Phone))
            {
                ViewBag.Error = "Số điện thoại cá nhân không hợp lệ (phải bắt đầu bằng 03, 05, 07, 08, 09 và đủ 10 số).";
                ViewBag.Guardian = await _context.Guardians.FirstOrDefaultAsync(g => g.TenantId == id);
                ViewBag.Roles = await _context.Roles.ToListAsync();
                return View("~/Views/Khiemndhe/EditUser.cshtml", user);
            }

            if (!string.IsNullOrEmpty(guardianPhone) && !IsValidPhoneNumber(guardianPhone))
            {
                ViewBag.Error = "Số điện thoại người bảo hộ không hợp lệ (phải bắt đầu bằng 03, 05, 07, 08, 09 và đủ 10 số).";
                ViewBag.Guardian = await _context.Guardians.FirstOrDefaultAsync(g => g.TenantId == id);
                ViewBag.Roles = await _context.Roles.ToListAsync();
                return View("~/Views/Khiemndhe/EditUser.cshtml", user);
            }

            user.FullName = string.IsNullOrEmpty(FullName) ? user.FullName : FullName;
            user.Email = string.IsNullOrEmpty(Email) ? user.Email : Email;
            user.Phone = string.IsNullOrEmpty(Phone) ? user.Phone : Phone;
            user.IdentityNumber = string.IsNullOrEmpty(IdentityNumber) ? user.IdentityNumber : IdentityNumber;
            user.IsActive = IsActive;
            user.RoleId = RoleId;

            var guardian = await _context.Guardians.FirstOrDefaultAsync(g => g.TenantId == id);
            if (guardian == null)
            {
                if (!string.IsNullOrEmpty(guardianName))
                {
                    guardian = new Guardian
                    {
                        Id = Guid.NewGuid(),
                        TenantId = id,
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
            return RedirectToAction("Index");
        }

        [HttpPost("Delete/{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var currentUserRole = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            if (currentUserRole?.ToLower() != "admin") return RedirectToAction("Index", "Home");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user != null)
            {
                user.IsActive = false;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }
    }
}
