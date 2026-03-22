using Microsoft.AspNetCore.Mvc;

namespace PROJECT_TEAM1_PRN222.Controllers.Datmthe176706.Users
{
 
    public class UsersController : Controller
    {
        public IActionResult Index()
        {
            // Nó sẽ tìm đến file Views/Users/Index.cshtml
            return View("~/Views/Datmthe176706/Users/Index.cshtml");
        }
    }
}
