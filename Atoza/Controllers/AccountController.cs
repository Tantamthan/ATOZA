using ATOZA.Application.Abstractions.Services;
using ATOZA.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Atoza_Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;

        public AccountController(IAuthService authService)
        {
            _authService = authService;
        }

        // =====================================================
        // ĐĂNG KÝ
        // =====================================================

        public IActionResult Register() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (!ModelState.IsValid) return View(dto);

            var (success, error) = await _authService.RegisterAsync(dto);
            if (!success) { ViewBag.Error = error; return View(dto); }

            return RedirectToAction("Login");
        }

        // =====================================================
        // ĐĂNG NHẬP
        // =====================================================

        public IActionResult Login()
        {
            // Tự đăng nhập nếu đã có Session
            if (HttpContext.Session.GetString("Role") != null)
                return RedirectByRole(HttpContext.Session.GetString("Role")!);
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            if (!ModelState.IsValid) return View(dto);

            var profile = await _authService.LoginAsync(dto.UserName, dto.Password);
            if (profile == null) { ViewBag.Error = "Sai tài khoản hoặc mật khẩu"; return View(dto); }

            // Lưu Session
            HttpContext.Session.SetInt32("IdUser", profile.Id);
            HttpContext.Session.SetString("FullName", profile.FullName);
            HttpContext.Session.SetString("Role", profile.Role);

            // Cookie RememberMe
            if (dto.RememberMe)
            {
                Response.Cookies.Append("RememberMe_Username", profile.UserName,
                    new CookieOptions { Expires = DateTimeOffset.UtcNow.AddDays(30), HttpOnly = true });
                Response.Cookies.Append("RememberMe_Hash", profile.PasswordHash,
                    new CookieOptions { Expires = DateTimeOffset.UtcNow.AddDays(30), HttpOnly = true });
            }

            return RedirectByRole(profile.Role);
        }

        // =====================================================
        // ĐĂNG XUẤT
        // =====================================================

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData.Clear();
            Response.Cookies.Delete("RememberMe_Username");
            Response.Cookies.Delete("RememberMe_Hash");
            return RedirectToAction("Index", "Home");
        }

        // =====================================================
        // PRIVATE
        // =====================================================

        private IActionResult RedirectByRole(string role) =>
            role == "Teacher"
                ? RedirectToAction("Index", "Teacher")
                : RedirectToAction("Index", "Student");
    }
}
