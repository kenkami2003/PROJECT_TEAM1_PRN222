using BoardingHouseManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROJECT_TEAM1_PRN222.ViewModel;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

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

    public async Task<IActionResult> PaymentList(string searchName, string typeFilter, string statusFilter)
    {
        var resList = _context.Reservations.Include(r => r.Guest)
            .Select(r => new PaymentVerificationViewModel
            {
                Id = r.Id,
                FullName = r.Guest.FullName,
                ImagePath = r.PaymentProofImage,
                Type = "Reservation",
                CreatedAt = r.CreatedAt,
                IsConfirmed = (r.Status == ReservationStatus.Confirmed)
            }).AsQueryable();

        var conList = _context.Contracts.Include(c => c.Tenan)
            .Select(c => new PaymentVerificationViewModel
            {
                Id = c.Id,
                FullName = c.Tenan.FullName,
                ImagePath = c.ContractProofImage,
                Type = "Contract",
                CreatedAt = c.StartDate, 
                IsConfirmed = c.IsActive
            }).AsQueryable();

        var allPayments = resList.Union(conList);

        if (!string.IsNullOrEmpty(searchName))
        {
            allPayments = allPayments.Where(p => p.FullName.Contains(searchName));
        }
        if (!string.IsNullOrEmpty(typeFilter))
        {
            allPayments = allPayments.Where(p => p.Type == typeFilter);
        }
        if (statusFilter == "Confirmed")
        {
            allPayments = allPayments.Where(p => p.IsConfirmed == true);
        }
        else if (statusFilter == "Pending")
        {
            allPayments = allPayments.Where(p => p.IsConfirmed == false);
        }

        var result = allPayments.OrderByDescending(p => p.CreatedAt).ToList();
        return View("~/Views/Datmthe176706/Admin/PaymentList.cshtml", result);
    }
}
