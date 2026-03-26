using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Linq;

namespace PROJECT_TEAM1_PRN222.Controllers
{
    [Authorize]
    [Route("Admin/[controller]")]
    public class PortalController : Controller
    {
        [Route("")]
        [Route("Index")]
        public IActionResult Index()
        {
            var currentUserRole = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            if (currentUserRole?.ToLower() != "admin") return RedirectToAction("Index", "Home");

            return View();
        }
    }
}
