using BoardingHouseManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PROJECT_TEAM1_PRN222.Controllers.Datmthe176706.Admin;


public class DashboardsController : Controller
{
    public IActionResult Index()
    {
        ViewBag.Total = _context.MaintenanceRequests.Count();
        ViewBag.Open = _context.MaintenanceRequests.Count(x => x.Status == RequestStatus.Open);
        ViewBag.Processing = _context.MaintenanceRequests.Count(x => x.Status == RequestStatus.Processing);
        ViewBag.Fixed = _context.MaintenanceRequests.Count(x => x.Status == RequestStatus.Fixed);

        return View("~/Views/Datmthe176706/Admin/Index.cshtml");
    }
}
