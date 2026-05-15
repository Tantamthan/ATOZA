using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Atoza_Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            string? role = HttpContext.Session.GetString("Role")
                ?? User.FindFirstValue(ClaimTypes.Role);

            if (role == "Teacher") return RedirectToAction("Index", "Teacher");
            if (role == "Student") return RedirectToAction("Index", "Student");
            if (role == "Admin") return RedirectToAction("Index", "Admin");

            return View();
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View();
    }
}
