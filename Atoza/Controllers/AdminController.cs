using ATOZA.Application.Abstractions.Services;
using ATOZA.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Atoza_Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _adminService.GetDashboardStatsAsync());
        }

        public async Task<IActionResult> UserList(UserRole? roleFilter)
        {
            ViewBag.RoleFilter = roleFilter?.ToString() ?? string.Empty;
            return View(await _adminService.GetAllUsersAsync(roleFilter));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(int userId, bool isActive)
        {
            if (!isActive && userId == CurrentUserId)
            {
                TempData["Error"] = "Khong the khoa tai khoan dang su dung.";
                return RedirectToAction("UserList");
            }

            bool updated = await _adminService.SetUserActiveStatusAsync(userId, isActive);
            TempData[updated ? "Success" : "Error"] = updated
                ? "Da cap nhat trang thai tai khoan."
                : "Khong tim thay tai khoan.";

            return RedirectToAction("UserList");
        }

        public async Task<IActionResult> ExamList()
        {
            return View(await _adminService.GetAllExamsWithCreatorAsync());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteExam(int examId)
        {
            bool deleted = await _adminService.DeleteExamAsync(examId);
            TempData[deleted ? "Success" : "Error"] = deleted
                ? "Da xoa de thi."
                : "Khong tim thay de thi.";

            return RedirectToAction("ExamList");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleExamVisibility(int examId, bool isPublic)
        {
            bool updated = await _adminService.SetExamPublicStatusAsync(examId, isPublic);
            TempData[updated ? "Success" : "Error"] = updated
                ? "Da cap nhat trang thai de thi."
                : "Khong tim thay de thi.";

            return RedirectToAction("ExamList");
        }

        public async Task<IActionResult> ClassList()
        {
            return View(await _adminService.GetAllClassesOverviewAsync());
        }

        private int CurrentUserId =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
                ? userId
                : HttpContext.Session.GetInt32("IdUser") ?? 0;
    }
}
