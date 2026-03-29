using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BoardingHouseManagement.Models;

namespace PROJECT_TEAM1_PRN222.Controllers.KhiemndheController
{
    [Authorize]
    [Route("Tickets")]
    public class TicketsController : Controller
    {
        private readonly AppDbContext _context;

        public TicketsController(AppDbContext context)
        {
            _context = context;
        }

        private Guid GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (idClaim != null && Guid.TryParse(idClaim.Value, out Guid parsedId))
            {
                return parsedId;
            }
            return Guid.Empty;
        }

        private string GetCurrentUserRole()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value?.ToLower() ?? "";
        }

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            var role = GetCurrentUserRole();
            var userId = GetCurrentUserId();

            IQueryable<SupportTicket> query = _context.SupportTickets
                .Include(t => t.Tenant);

            // Nếu không phải Admin hoặc Staff thì chỉ xem ticket của mình
            if (role != "admin" && role != "staff")
            {
                query = query.Where(t => t.TenantId == userId);
            }

            var tickets = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
            return View("~/Views/Khiemndhe/TicketsIndex.cshtml", tickets);
        }

        [HttpGet("Create")]
        public IActionResult Create()
        {
            var role = GetCurrentUserRole();
            if (role == "admin" || role == "staff")
            {
                // Admin không tạo khiếu nại
                return RedirectToAction("Index");
            }
            return View("~/Views/Khiemndhe/CreateTicket.cshtml");
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string title, string description)
        {
            var role = GetCurrentUserRole();
            if (role == "admin" || role == "staff")
            {
                return RedirectToAction("Index");
            }

            if(string.IsNullOrEmpty(title) || string.IsNullOrEmpty(description))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ tiêu đề và nội dung.";
                return View("~/Views/Khiemndhe/CreateTicket.cshtml");
            }

            var ticket = new SupportTicket
            {
                Id = Guid.NewGuid(),
                TenantId = GetCurrentUserId(),
                Title = title,
                Description = description,
                Status = TicketStatus.Pending,
                CreatedAt = DateTime.Now
            };

            _context.SupportTickets.Add(ticket);

            // Gửi thông báo cho toàn bộ Admin và Staff
            var staffAndAdmins = await _context.Users
                .Include(u => u.Role)
                .Where(u => u.Role.RoleName.ToLower() == "admin" || u.Role.RoleName.ToLower() == "staff")
                .ToListAsync();

            var tenantName = User.Identity?.Name ?? "Một khách hàng";

            foreach (var sa in staffAndAdmins)
            {
                var notif = new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = sa.Id,
                    Title = "Khiếu nại/Hỗ trợ mới",
                    Message = $"{tenantName} vừa gửi một yêu cầu hỗ trợ mới: {title}",
                    ActionLink = $"/Tickets/Details/{ticket.Id}"
                };
                _context.Notifications.Add(notif);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Gửi yêu cầu thành công!";
            return RedirectToAction("Index");
        }

        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(Guid id)
        {
            var role = GetCurrentUserRole();
            var userId = GetCurrentUserId();

            var ticket = await _context.SupportTickets
                .Include(t => t.Tenant)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null) return NotFound();

            if (role != "admin" && role != "staff" && ticket.TenantId != userId)
            {
                return Forbid();
            }

            return View("~/Views/Khiemndhe/TicketDetails.cshtml", ticket);
        }

        [HttpPost("Reply/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(Guid id, string adminReply, TicketStatus status)
        {
            var role = GetCurrentUserRole();
            if (role != "admin" && role != "staff")
            {
                return Forbid();
            }

            var ticket = await _context.SupportTickets.FindAsync(id);
            if (ticket == null) return NotFound();

            ticket.AdminReply = adminReply;
            ticket.Status = status;
            ticket.UpdatedAt = DateTime.Now;

            // Thông báo cho Tenant biết ticket của họ được cập nhật
            var notif = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = ticket.TenantId,
                Title = "Cập nhật yêu cầu hỗ trợ",
                Message = $"Yêu cầu hỗ trợ \"{ticket.Title}\" của bạn đã được phản hồi.",
                ActionLink = $"/Tickets/Details/{ticket.Id}"
            };
            _context.Notifications.Add(notif);

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã cập nhật và phản hồi ticket thành công.";

            return RedirectToAction("Details", new { id = id });
        }
    }
}
