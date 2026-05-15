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
        private const string GoogleProvider = "Google";
        private const string ExternalCookieScheme = "Atoza.External";
        private const string GoogleEmailSessionKey = "GoogleLogin:Email";
        private const string GoogleFullNameSessionKey = "GoogleLogin:FullName";

        private readonly IAuthService _authService;
        private readonly IAuthenticationSchemeProvider _schemeProvider;

        public AccountController(IAuthService authService, IAuthenticationSchemeProvider schemeProvider)
        {
            _authService = authService;
            _schemeProvider = schemeProvider;
        }

        [AllowAnonymous]
        public IActionResult Register()
        {
            var googleEmail = HttpContext.Session.GetString(GoogleEmailSessionKey);
            if (!string.IsNullOrWhiteSpace(googleEmail))
            {
                return View(new RegisterDto
                {
                    Email = googleEmail,
                    FullName = HttpContext.Session.GetString(GoogleFullNameSessionKey) ?? string.Empty,
                    UserName = SuggestUserName(googleEmail),
                    Role = "Student",
                    IsExternalLogin = true
                });
            }

            return View(new RegisterDto());
        }

        [HttpPost, ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            var googleEmail = HttpContext.Session.GetString(GoogleEmailSessionKey);
            var isGoogleRegistration = !string.IsNullOrWhiteSpace(googleEmail);
            if (isGoogleRegistration)
            {
                dto.IsExternalLogin = true;
                dto.Email = googleEmail!;
                ModelState.Remove(nameof(RegisterDto.Email));
                ModelState.Remove(nameof(RegisterDto.Password));
                ModelState.Remove(nameof(RegisterDto.ConfirmPassword));
            }

            if (!ModelState.IsValid) return View(dto);
            if (string.Equals(dto.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(dto.Role), "Khong the dang ky tai khoan Admin.");
                return View(dto);
            }

            if (isGoogleRegistration)
            {
                var (googleSuccess, googleError, profile) = await _authService.RegisterWithGoogleAsync(dto, googleEmail!);
                if (!googleSuccess)
                {
                    ViewBag.Error = googleError;
                    return View(dto);
                }

                ClearGoogleRegistrationSession();

                if (profile == null)
                {
                    TempData["AuthMessage"] = "Tai khoan giao vien da duoc tao va dang cho Admin duyet.";
                    return RedirectToAction("Login");
                }

                await SignInApplicationAsync(profile, rememberMe: false);
                return RedirectByRole(profile.Role);
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

            await SignInApplicationAsync(profile, dto.RememberMe);

            return RedirectByRole(profile.Role);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLogin(string provider, string? returnUrl = null)
        {
            if (!string.Equals(provider, GoogleProvider, StringComparison.OrdinalIgnoreCase) ||
                await _schemeProvider.GetSchemeAsync(provider) == null)
            {
                TempData["AuthError"] = "Dang nhap Google chua duoc cau hinh.";
                return RedirectToAction("Login");
            }

            ClearGoogleRegistrationSession();

            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, provider);
        }

        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
        {
            if (!string.IsNullOrWhiteSpace(remoteError))
            {
                TempData["AuthError"] = "Google khong xac thuc duoc tai khoan.";
                return RedirectToAction("Login");
            }

            var externalResult = await HttpContext.AuthenticateAsync(ExternalCookieScheme);
            if (!externalResult.Succeeded || externalResult.Principal == null)
            {
                TempData["AuthError"] = "Khong doc duoc thong tin dang nhap Google.";
                return RedirectToAction("Login");
            }

            var email = externalResult.Principal.FindFirstValue(ClaimTypes.Email);
            var fullName = externalResult.Principal.FindFirstValue(ClaimTypes.Name)
                ?? externalResult.Principal.FindFirstValue(ClaimTypes.GivenName)
                ?? string.Empty;

            await HttpContext.SignOutAsync(ExternalCookieScheme);

            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["AuthError"] = "Tai khoan Google khong co email hop le.";
                return RedirectToAction("Login");
            }

            var profile = await _authService.LoginWithGoogleAsync(email);
            if (profile != null)
            {
                ClearGoogleRegistrationSession();
                await SignInApplicationAsync(profile, rememberMe: false);
                return RedirectByRole(profile.Role);
            }

            if (await _authService.IsEmailRegisteredAsync(email))
            {
                TempData["AuthError"] = "Tai khoan voi email Google nay chua duoc kich hoat hoac chua duoc duyet.";
                return RedirectToAction("Login");
            }

            StoreGoogleRegistrationSession(email, fullName);
            return RedirectToAction("Register");
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignOutAsync(ExternalCookieScheme);
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

        private async Task SignInApplicationAsync(UserProfileDto profile, bool rememberMe)
        {
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
                IsPersistent = rememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.Add(rememberMe
                    ? TimeSpan.FromDays(30)
                    : TimeSpan.FromMinutes(30))
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                authProperties);

            StoreSession(profile);
        }

        private void StoreSession(UserProfileDto profile)
        {
            HttpContext.Session.SetInt32("IdUser", profile.Id);
            HttpContext.Session.SetString("FullName", profile.FullName);
            HttpContext.Session.SetString("Role", profile.Role);
        }

        private void StoreGoogleRegistrationSession(string email, string fullName)
        {
            HttpContext.Session.SetString(GoogleEmailSessionKey, email);
            HttpContext.Session.SetString(GoogleFullNameSessionKey, fullName);
        }

        private void ClearGoogleRegistrationSession()
        {
            HttpContext.Session.Remove(GoogleEmailSessionKey);
            HttpContext.Session.Remove(GoogleFullNameSessionKey);
        }

        private static string SuggestUserName(string email)
        {
            var localPart = email.Split('@')[0];
            var allowed = localPart
                .Where(c => char.IsLetterOrDigit(c) || c == '.' || c == '_' || c == '-')
                .ToArray();

            return allowed.Length == 0 ? string.Empty : new string(allowed);
        }
    }
}
