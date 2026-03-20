using Microsoft.AspNetCore.Mvc;

namespace PROJECT_TEAM1_PRN222.Controllers.Datmthe176706.Admin;

public class DashboardsController : Controller
{
    [Area("Datmthe176706")]
    public IActionResult Index()
    {
        return View("~/Views/Datmthe176706/Admin/Index.cshtml");
    }
}
