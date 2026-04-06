using Microsoft.AspNetCore.Mvc;

namespace Atoza_Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // Nếu đã đăng nhập thì chuyển hướng về đúng trang
            string? role = HttpContext.Session.GetString("Role");
            if (role == "Teacher") return RedirectToAction("Index", "Teacher");
            if (role == "Student") return RedirectToAction("Index", "Student");
            return View();
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View();
    }
}
