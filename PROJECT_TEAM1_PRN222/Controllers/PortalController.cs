using Microsoft.AspNetCore.Mvc;

namespace PROJECT_TEAM1_PRN222.Controllers
{
    public class PortalController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
