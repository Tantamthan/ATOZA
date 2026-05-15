using ATOZA.Application.Abstractions.Services;
using ATOZA.Application.DTOs.Exam;
using ATOZA.Application.DTOs.Submission;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Atoza_Web.Controllers
{
    [Authorize]
    public class ExamController : Controller
    {
        private readonly IExamService _examService;
        private readonly ISubmissionService _submissionService;

        public ExamController(IExamService examService, ISubmissionService submissionService)
        {
            _examService = examService;
            _submissionService = submissionService;
        }

        private int UserId =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
                ? userId
                : HttpContext.Session.GetInt32("IdUser") ?? 0;

        [Authorize(Roles = "Teacher")]
        public IActionResult CreateExam()
        {
            ViewBag.RawContent = HttpContext.Session.GetString("UploadedRawContent");
            HttpContext.Session.Remove("UploadedRawContent");
            ViewBag.Error = TempData["Error"];
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> SaveExamApi([FromBody] CreateExamDto dto)
        {
            if (dto == null || !ModelState.IsValid)
                return BadRequest(new { success = false, message = "Du lieu khong hop le." });

            if (UserId <= 0)
                return Unauthorized(new { success = false, message = "Phien dang nhap khong hop le." });

            try
            {
                int examId = await _examService.CreateExamAsync(dto, UserId);
                return Ok(new { success = true, examId });
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { success = false, message = "Khong the luu de thi luc nay." });
            }
        }

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Index(int? id)
        {
            if (id == null) return NotFound();

            var access = await _examService.GetExamForStudentAsync(id.Value, UserId);
            if (!access.Success)
            {
                TempData["Error"] = access.Message;
                return RedirectToAction("MyAssignments", "Student");
            }

            return View(access.Exam);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> SubmitExam([FromBody] SubmitExamDto dto)
        {
            if (dto == null || !ModelState.IsValid)
                return BadRequest(new { success = false, message = "Du lieu khong hop le." });

            if (UserId <= 0)
                return Unauthorized(new { success = false, message = "Phien dang nhap khong hop le." });

            var result = await _submissionService.SubmitExamAsync(dto, UserId);
            return Ok(new
            {
                success = result.Success,
                score = result.Score,
                message = result.Message,
                redirectUrl = result.Success ? Url.Action("MyAssignments", "Student") : null
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> CheckPracticeAnswer([FromBody] CheckPracticeAnswerDto dto)
        {
            if (dto == null || !ModelState.IsValid)
                return BadRequest(new { success = false, message = "Du lieu khong hop le." });

            if (UserId <= 0)
                return Unauthorized(new { success = false, message = "Phien dang nhap khong hop le." });

            var result = await _examService.CheckPracticeAnswerAsync(dto, UserId);
            if (!result.Success)
                return BadRequest(new { success = false, message = result.Message });

            return Ok(new
            {
                success = true,
                isCorrect = result.IsCorrect,
                correctAnswer = result.CorrectAnswer,
                message = result.Message
            });
        }

        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> EditExam(int id)
        {
            var exam = await _examService.GetExamForEditAsync(id, UserId);
            if (exam == null)
            {
                TempData["Error"] = "Khong tim thay de thi hoac ban khong co quyen chinh sua.";
                return RedirectToAction("Index", "Teacher");
            }

            // Chuyển questions thành raw text format cho textarea
            var rawLines = new System.Text.StringBuilder();
            var sortedQuestions = exam.Questions.OrderBy(q => q.OrderNumber).ToList();
            for (int i = 0; i < sortedQuestions.Count; i++)
            {
                var q = sortedQuestions[i];
                rawLines.AppendLine($"Cau {i + 1}: {q.Content}");

                void AppendOption(string key, string content, string correctAnswer)
                {
                    var marker = key == correctAnswer ? "*" : "";
                    rawLines.AppendLine($"{marker}{key}. {content}");
                }

                AppendOption("A", q.OptionA, q.CorrectAnswer);
                AppendOption("B", q.OptionB, q.CorrectAnswer);
                AppendOption("C", q.OptionC, q.CorrectAnswer);
                AppendOption("D", q.OptionD, q.CorrectAnswer);
                rawLines.AppendLine();
            }

            ViewBag.RawContent = rawLines.ToString().TrimEnd();
            ViewBag.IsEditMode = true;
            ViewBag.EditExamId = exam.Id;
            ViewBag.EditExamTitle = exam.Title;
            ViewBag.EditExamDuration = exam.DurationMinutes;
            ViewBag.EditExamMode = exam.ExamMode.ToString();
            ViewBag.EditExamIsPublic = exam.IsPublic;

            return View("CreateExam");
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> UpdateExamApi([FromBody] UpdateExamDto dto)
        {
            if (dto == null || !ModelState.IsValid)
                return BadRequest(new { success = false, message = "Du lieu khong hop le." });

            if (UserId <= 0)
                return Unauthorized(new { success = false, message = "Phien dang nhap khong hop le." });

            try
            {
                bool updated = await _examService.UpdateExamAsync(dto, UserId);
                if (!updated)
                    return NotFound(new { success = false, message = "Khong tim thay de thi hoac ban khong co quyen." });

                return Ok(new { success = true, examId = dto.ExamId });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateExamApi] Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[UpdateExamApi] Inner: {ex.InnerException?.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { success = false, message = "Khong the cap nhat de thi luc nay. Chi tiet: " + ex.InnerException?.Message ?? ex.Message });
            }
        }
    }
}
