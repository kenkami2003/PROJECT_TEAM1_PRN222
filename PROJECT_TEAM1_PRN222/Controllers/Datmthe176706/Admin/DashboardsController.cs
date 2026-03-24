using Microsoft.AspNetCore.Mvc;

namespace PROJECT_TEAM1_PRN222.Controllers.Datmthe176706.Admin
{
    [Route("Datmthe176706/Admin/[controller]")]
    public class DashboardsController : Controller
    {
        [HttpGet("")]
        [HttpGet("Index")]
        public IActionResult Index()
        {
            return View("~/Views/Datmthe176706/Admin/Index.cshtml");
        }
    }
}