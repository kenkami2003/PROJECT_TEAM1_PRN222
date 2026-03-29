using BoardingHouseManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROJECT_TEAM1_PRN222.ViewModel;

namespace PROJECT_TEAM1_PRN222.Controllers.Datmthe176706.Admin;

[Area("Datmthe176706")]
public class DashboardsController : Controller
{
    private readonly AppDbContext _context;

    public DashboardsController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return View("~/Views/Datmthe176706/Admin/Index.cshtml");
    }

    [HttpGet]
    public async Task<IActionResult> PaymentList(string searchName, string typeFilter, string statusFilter)
    {
        List<PaymentVerificationViewModel> payments = new List<PaymentVerificationViewModel>();

        // 1. Lấy Biên lai giữ chỗ
        var reservations = await _context.Reservations
            .Include(r => r.Guest)
            .Where(r => r.PaymentProofImage != null)
            .ToListAsync();

        foreach (var r in reservations)
        {
            payments.Add(new PaymentVerificationViewModel
            {
                Id = r.Id,
                FullName = r.Guest?.FullName ?? "Unknown",
                ImagePath = r.PaymentProofImage,
                Type = "Reservation",
                Amount = r.ReservationFee,
                CreatedAt = r.CreatedAt,
                IsConfirmed = r.Status == ReservationStatus.Confirmed,
                Status = r.Status == ReservationStatus.Confirmed ? "Confirmed" : "Pending"
            });
        }

        // 2. Lấy Biên lai hợp đồng chính thức
        var contracts = await _context.Contracts
            .Include(c => c.Tenan)
            .Where(c => c.ContractProofImage != null)
            .ToListAsync();

        foreach (var c in contracts)
        {
            payments.Add(new PaymentVerificationViewModel
            {
                Id = c.Id,
                FullName = c.Tenan?.FullName ?? "Unknown",
                ImagePath = c.ContractProofImage,
                Type = "Contract",
                Amount = c.ActualDeposit,
                CreatedAt = c.CreatedAt,
                IsConfirmed = c.IsActive,
                Status = c.IsActive ? "Confirmed" : "Pending"
            });
        }

        // 3. Áp dụng bộ lọc
        var query = payments.AsEnumerable();

        if (!string.IsNullOrEmpty(searchName))
        {
            query = query.Where(p => p.FullName.Contains(searchName, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(typeFilter))
        {
            query = query.Where(p => p.Type == typeFilter);
        }

        if (!string.IsNullOrEmpty(statusFilter))
        {
            query = query.Where(p => p.Status == statusFilter);
        }

        // 4. Sắp xếp mới nhất lên trên
        query = query.OrderByDescending(p => p.CreatedAt);

        return View("~/Views/Datmthe176706/Admin/PaymentList.cshtml", query.ToList());
    }
}
