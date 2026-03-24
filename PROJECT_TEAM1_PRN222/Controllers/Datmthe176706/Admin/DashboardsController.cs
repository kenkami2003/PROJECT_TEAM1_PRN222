using BoardingHouseManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PROJECT_TEAM1_PRN222.Controllers.Datmthe176706.Admin;


public class DashboardsController : Controller
{
    public IActionResult Index()
    {
        
        return View("~/Views/Datmthe176706/Admin/Index.cshtml");
    }
}
