using ATOZA.Application.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;

namespace Atoza_Web.Controllers
{
    public class StudentController : Controller
    {
        private readonly IClassService _classService;
        private readonly ISubmissionService _submissionService;

        public StudentController(IClassService classService, ISubmissionService submissionService)
        {
            _classService = classService;
            _submissionService = submissionService;
        }

        private bool IsStudent() =>
            HttpContext.Session.GetString("Role") == "Student";

        private int StudentId =>
            HttpContext.Session.GetInt32("IdUser") ?? 0;

        // Dashboard: Danh sách lớp đã tham gia
        public async Task<IActionResult> Index()
        {
            if (!IsStudent()) return RedirectToAction("Login", "Account");
            return View(await _classService.GetClassesByStudentAsync(StudentId));
        }

        // Tham gia lớp bằng JoinCode
        public IActionResult JoinClass()
        {
            if (!IsStudent()) return RedirectToAction("Login", "Account");
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> JoinClass(string joinCode)
        {
            if (!IsStudent()) return RedirectToAction("Login", "Account");
            if (string.IsNullOrEmpty(joinCode)) { ViewBag.Error = "Vui lòng nhập mã lớp."; return View(); }

            var (success, error) = await _classService.JoinClassAsync(joinCode, StudentId);
            if (!success) { ViewBag.Error = error; return View(); }

            TempData["Success"] = "Tham gia lớp thành công!";
            return RedirectToAction("Index");
        }

        // Chi tiết lớp – danh sách bài tập
        public async Task<IActionResult> ClassDetail(int id)
        {
            if (!IsStudent()) return RedirectToAction("Login", "Account");
            var assignments = await _classService.GetAssignmentsForStudentAsync(id, StudentId);
            if (assignments == null) { TempData["Error"] = "Bạn không phải thành viên lớp này!"; return RedirectToAction("Index"); }
            return View(assignments);
        }

        // Lịch sử bài đã nộp
        public async Task<IActionResult> MyAssignments()
        {
            if (!IsStudent()) return RedirectToAction("Login", "Account");
            return View(await _submissionService.GetStudentSubmissionsAsync(StudentId));
        }

        // Xem lại bài đã làm
        public async Task<IActionResult> ReviewExam(int id)
        {
            if (!IsStudent()) return RedirectToAction("Login", "Account");
            var submission = await _submissionService.GetSubmissionDetailAsync(id, StudentId);
            if (submission == null) { TempData["Error"] = "Bạn chưa làm bài này."; return RedirectToAction("MyAssignments"); }
            return View(submission);
        }
    }
}
