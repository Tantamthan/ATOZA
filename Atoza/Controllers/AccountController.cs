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
        private readonly IEmailSender _emailSender;
        private readonly IAuthenticationSchemeProvider _schemeProvider;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            IAuthService authService,
            IEmailSender emailSender,
            IAuthenticationSchemeProvider schemeProvider,
            ILogger<AccountController> logger)
        {
            _authService = authService;
            _emailSender = emailSender;
            _schemeProvider = schemeProvider;
            _logger = logger;
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
                ModelState.AddModelError(nameof(dto.Role), "Không thể đăng ký tài khoản Admin.");
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
                    TempData["AuthMessage"] = "Tài khoản giáo viên đã được tạo và đang chờ Admin duyệt.";
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
                ViewBag.Error = "Sai tài khoản hoặc mật khẩu.";
                return View(dto);
            }

            await SignInApplicationAsync(profile, dto.RememberMe);

            return RedirectByRole(profile.Role);
        }

        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordDto());
        }

        [HttpPost, ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
        {
            if (!ModelState.IsValid) return View(dto);

            if (!_emailSender.IsConfigured)
            {
                ViewBag.SmtpMissing = true;
                return View(dto);
            }

            var result = await _authService.RequestPasswordResetAsync(dto.Email);
            ViewBag.RequestSent = true;

            if (!string.IsNullOrWhiteSpace(result.ResetToken))
            {
                var resetLink = Url.Action(
                    nameof(ResetPassword),
                    "Account",
                    new { token = result.ResetToken },
                    Request.Scheme);

                if (_emailSender.IsConfigured && !string.IsNullOrWhiteSpace(resetLink))
                {
                    try
                    {
                        await _emailSender.SendPasswordResetAsync(dto.Email.Trim(), resetLink, HttpContext.RequestAborted);
                        ViewBag.EmailSent = true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Không gửi được email đặt lại mật khẩu đến {Email}.", dto.Email);
                        ViewBag.EmailSendFailed = true;
                    }
                }
            }

            return View(dto);
        }

        [AllowAnonymous]
        public IActionResult ResetPassword(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                TempData["AuthError"] = "Liên kết đặt lại mật khẩu không hợp lệ.";
                return RedirectToAction("Login");
            }

            return View(new ResetPasswordDto { Token = token });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            if (!ModelState.IsValid) return View(dto);

            var (success, error) = await _authService.ResetPasswordAsync(dto);
            if (!success)
            {
                ViewBag.Error = error;
                return View(dto);
            }

            TempData["AuthMessage"] = "Mật khẩu đã được cập nhật. Vui lòng đăng nhập lại.";
            return RedirectToAction("Login");
        }

        [HttpPost, ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLogin(string provider, string? returnUrl = null)
        {
            if (!string.Equals(provider, GoogleProvider, StringComparison.OrdinalIgnoreCase) ||
                await _schemeProvider.GetSchemeAsync(provider) == null)
            {
                TempData["AuthError"] = "Đăng nhập Google chưa được cấu hình.";
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
                TempData["AuthError"] = "Google không xác thực được tài khoản.";
                return RedirectToAction("Login");
            }

            var externalResult = await HttpContext.AuthenticateAsync(ExternalCookieScheme);
            if (!externalResult.Succeeded || externalResult.Principal == null)
            {
                TempData["AuthError"] = "Không đọc được thông tin đăng nhập Google.";
                return RedirectToAction("Login");
            }

            var email = externalResult.Principal.FindFirstValue(ClaimTypes.Email);
            var fullName = externalResult.Principal.FindFirstValue(ClaimTypes.Name)
                ?? externalResult.Principal.FindFirstValue(ClaimTypes.GivenName)
                ?? string.Empty;

            await HttpContext.SignOutAsync(ExternalCookieScheme);

            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["AuthError"] = "Tài khoản Google không có email hợp lệ.";
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
                TempData["AuthError"] = "Tài khoản với email Google này chưa được kích hoạt hoặc chưa được duyệt.";
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
