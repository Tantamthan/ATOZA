using ATOZA.Application.Abstractions.Services;
using ATOZA.Application.DTOs.Exam;
using ATOZA.Application.DTOs.Submission;
using Microsoft.AspNetCore.Mvc;

namespace Atoza_Web.Controllers
{
    public class ExamController : Controller
    {
        private readonly IExamService _examService;
        private readonly ISubmissionService _submissionService;

        public ExamController(IExamService examService, ISubmissionService submissionService)
        {
            _examService = examService;
            _submissionService = submissionService;
        }

        private int UserId => HttpContext.Session.GetInt32("IdUser") ?? 0;

        // =====================================================
        // TẠO ĐỀ THI (View)
        // =====================================================

        public IActionResult CreateExam()
        {
            ViewBag.RawContent = TempData["RawContent"];
            ViewBag.Error = TempData["Error"];
            return View();
        }

        // =====================================================
        // LƯU ĐỀ THI (API endpoint)
        // =====================================================

        [HttpPost]
        public async Task<IActionResult> SaveExamApi([FromBody] CreateExamDto dto)
        {
            if (dto == null || !ModelState.IsValid)
                return Json(new { success = false, message = "Dữ liệu lỗi." });

            try
            {
                int examId = await _examService.CreateExamAsync(dto, UserId);
                return Json(new { success = true, examId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // =====================================================
        // LÀM BÀI THI
        // =====================================================

        public async Task<IActionResult> Index(int? id)
        {
            if (HttpContext.Session.GetString("Role") == null)
                return RedirectToAction("Login", "Account");
            if (id == null) return NotFound();

            var exam = await _examService.GetExamWithQuestionsAsync(id.Value);
            if (exam == null) return NotFound();

            if (await _examService.HasSubmittedAsync(id.Value, UserId))
            {
                TempData["Error"] = "Bạn đã nộp bài thi này rồi.";
                return RedirectToAction("MyAssignments", "Student");
            }

            return View(exam);
        }

        // =====================================================
        // NỘP BÀI
        // =====================================================

        [HttpPost]
        public async Task<IActionResult> SubmitExam([FromBody] SubmitExamDto dto)
        {
            if (HttpContext.Session.GetString("Role") == null)
                return Json(new { success = false, message = "Hết phiên đăng nhập." });

            var result = await _submissionService.SubmitExamAsync(dto, UserId);
            return Json(new
            {
                success = result.Success,
                score = result.Score,
                message = result.Message,
                redirectUrl = result.Success ? Url.Action("MyAssignments", "Student") : null
            });
        }
    }
}
