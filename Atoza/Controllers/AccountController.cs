using ATOZA.Application.Abstractions.Services;
using ATOZA.Application.DTOs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Atoza_Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;

        public AccountController(IAuthService authService)
        {
            _authService = authService;
        }

        [AllowAnonymous]
        public IActionResult Register() => View();

        [HttpPost, ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (!ModelState.IsValid) return View(dto);
            if (string.Equals(dto.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(dto.Role), "Khong the dang ky tai khoan Admin.");
                return View(dto);
            }

            var (success, error) = await _authService.RegisterAsync(dto);
            if (!success) { ViewBag.Error = error; return View(dto); }

            return RedirectToAction("Login");
        }

        [AllowAnonymous]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectByRole(User.FindFirstValue(ClaimTypes.Role) ?? "Student");

            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            if (!ModelState.IsValid) return View(dto);

            var profile = await _authService.LoginAsync(dto.UserName, dto.Password);
            if (profile == null)
            {
                ViewBag.Error = "Sai tai khoan hoac mat khau";
                return View(dto);
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, profile.Id.ToString()),
                new(ClaimTypes.Name, profile.UserName),
                new(ClaimTypes.GivenName, profile.FullName),
                new(ClaimTypes.Email, profile.Email),
                new(ClaimTypes.Role, profile.Role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = dto.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.Add(dto.RememberMe
                    ? TimeSpan.FromDays(30)
                    : TimeSpan.FromMinutes(30))
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                authProperties);

            StoreSession(profile);

            return RedirectByRole(profile.Role);
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            TempData.Clear();
            Response.Cookies.Delete("RememberMe_Username");
            Response.Cookies.Delete("RememberMe_Hash");
            return RedirectToAction("Index", "Home");
        }

        private IActionResult RedirectByRole(string role) =>
            role == "Admin"
                ? RedirectToAction("Index", "Admin")
                : role == "Teacher"
                    ? RedirectToAction("Index", "Teacher")
                    : RedirectToAction("Index", "Student");

        private void StoreSession(UserProfileDto profile)
        {
            HttpContext.Session.SetInt32("IdUser", profile.Id);
            HttpContext.Session.SetString("FullName", profile.FullName);
            HttpContext.Session.SetString("Role", profile.Role);
        }
    }
}
