using ATOZA.Application.Abstractions.Services;
using ATOZA.Application.DTOs.Class;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Globalization;
using System.Security.Claims;

namespace Atoza_Web.Controllers
{
    [Authorize(Roles = "Teacher")]
    public class TeacherController : Controller
    {
        private readonly IClassService _classService;
        private readonly IExamService _examService;
        private readonly ISubmissionService _submissionService;
        private readonly IFileParserService _fileParser;
        private readonly ILogger<TeacherController> _logger;

        public TeacherController(
            IClassService classService,
            IExamService examService,
            ISubmissionService submissionService,
            IFileParserService fileParser,
            ILogger<TeacherController> logger)
        {
            _classService = classService;
            _examService = examService;
            _submissionService = submissionService;
            _fileParser = fileParser;
            _logger = logger;
        }

        private bool IsTeacher() => User.IsInRole("Teacher");

        private int TeacherId =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
                ? userId
                : HttpContext.Session.GetInt32("IdUser") ?? 0;

        public async Task<IActionResult> Index()
        {
            if (!IsTeacher()) return RedirectToAction("Login", "Account");

            var exams = await _examService.GetExamsByCreatorAsync(TeacherId);
            ViewBag.ClassCount = (await _classService.GetClassesByTeacherAsync(TeacherId)).Count;
            return View(exams);
        }

        public async Task<IActionResult> ClassList()
        {
            if (!IsTeacher()) return RedirectToAction("Login", "Account");
            return View(await _classService.GetClassesByTeacherAsync(TeacherId));
        }

        public IActionResult CreateClass()
        {
            if (!IsTeacher()) return RedirectToAction("Login", "Account");
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateClass(CreateClassDto dto)
        {
            if (!IsTeacher()) return RedirectToAction("Login", "Account");
            if (!ModelState.IsValid) return View(dto);

            await _classService.CreateClassAsync(dto, TeacherId);
            return RedirectToAction("ClassList");
        }

        public async Task<IActionResult> ClassDetail(int id)
        {
            if (!IsTeacher()) return RedirectToAction("Login", "Account");

            var cls = await _classService.GetClassDetailAsync(id, TeacherId);
            if (cls == null)
            {
                TempData["Error"] = "Khong tim thay lop hoc.";
                return RedirectToAction("ClassList");
            }

            return View(cls);
        }

        public async Task<IActionResult> ExportStudents(int classId)
        {
            if (!IsTeacher()) return RedirectToAction("Login", "Account");

            var data = await _classService.ExportStudentsCsvAsync(classId, TeacherId);
            if (data == null) return RedirectToAction("ClassList");
            return File(data, "text/csv", $"DanhSachHocSinh_{classId}.csv");
        }

        public async Task<IActionResult> AssignExam()
        {
            if (!IsTeacher()) return RedirectToAction("Login", "Account");

            await PopulateAssignExamListsAsync();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignExam(AssignExamDto dto, string? availableFromUtc, string? dueDateUtc)
        {
            if (!IsTeacher()) return RedirectToAction("Login", "Account");

            if (!TryParseUtcDateTime(availableFromUtc, out var availableFrom))
                ModelState.AddModelError(nameof(dto.AvailableFrom), "Thoi gian mo de khong hop le.");

            if (!TryParseUtcDateTime(dueDateUtc, out var dueDate))
                ModelState.AddModelError(nameof(dto.DueDate), "Han nop bai khong hop le.");

            if (ModelState.IsValid)
            {
                dto.AvailableFrom = availableFrom;
                dto.DueDate = dueDate;

                var result = await _classService.AssignExamAsync(dto, TeacherId);
                if (result.Success)
                {
                    TempData["Success"] = "Giao bai thanh cong!";
                    return RedirectToAction("Index");
                }

                ModelState.AddModelError("", result.Error ?? "Loi khong xac dinh");
            }

            await PopulateAssignExamListsAsync();
            return View(dto);
        }

        private static bool TryParseUtcDateTime(string? value, out DateTime utcDateTime)
        {
            if (DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
            {
                utcDateTime = parsed.UtcDateTime;
                return true;
            }

            utcDateTime = default;
            return false;
        }

        public async Task<IActionResult> ClassAssignmentsList(int classId)
        {
            if (!IsTeacher()) return RedirectToAction("Login", "Account");

            var cls = await _classService.GetClassDetailAsync(classId, TeacherId);
            if (cls == null)
            {
                TempData["Error"] = "Khong tim thay lop.";
                return RedirectToAction("ClassList");
            }

            ViewBag.ClassName = cls.ClassName;
            ViewBag.ClassId = classId;
            return View(await _classService.GetClassAssignmentsAsync(classId, TeacherId));
        }

        public IActionResult TeacherCreateExam()
        {
            if (!IsTeacher()) return RedirectToAction("Login", "Account");
            return View();
        }

        public async Task<IActionResult> ExamSubmissionReport(int classId, int examId)
        {
            if (!IsTeacher()) return RedirectToAction("Login", "Account");

            var cls = await _classService.GetClassDetailAsync(classId, TeacherId);
            if (cls == null) return RedirectToAction("ClassList");

            var report = await _submissionService.GetSubmissionReportAsync(classId, examId);
            ViewBag.ClassName = cls.ClassName;
            ViewBag.ClassId = classId;
            return View(report);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SetExamVisibility(int examId, bool isPublic)
        {
            if (!IsTeacher()) return RedirectToAction("Login", "Account");

            bool updated = await _examService.SetExamVisibilityAsync(examId, TeacherId, isPublic);
            TempData[updated ? "Success" : "Error"] = updated
                ? (isPublic ? "Da mo cong khai de thi." : "Da chuyen de thi ve rieng tu.")
                : "Khong tim thay de thi cua ban.";

            return RedirectToAction("Index");
        }

        [RequestSizeLimit(10 * 1024 * 1024)]
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult ProcessExamFile(IFormFile fileUpload)
        {
            if (!IsTeacher()) return RedirectToAction("Login", "Account");
            if (fileUpload == null || fileUpload.Length == 0)
            {
                TempData["Error"] = "Vui long chon file.";
                return RedirectToAction("CreateExam", "Exam");
            }

            const long maxFileSize = 10 * 1024 * 1024;
            if (fileUpload.Length > maxFileSize)
            {
                TempData["Error"] = "File khong duoc vuot qua 10MB.";
                return RedirectToAction("CreateExam", "Exam");
            }

            string ext = Path.GetExtension(fileUpload.FileName).ToLowerInvariant();
            var allowedExtensions = new HashSet<string> { ".docx", ".pdf" };
            if (!allowedExtensions.Contains(ext))
            {
                TempData["Error"] = "Chi ho tro file .docx va .pdf.";
                return RedirectToAction("CreateExam", "Exam");
            }

            var allowedMimeTypes = new Dictionary<string, string[]>
            {
                { ".docx", new[] { "application/vnd.openxmlformats-officedocument.wordprocessingml.document" } },
                { ".pdf", new[] { "application/pdf" } }
            };

            if (allowedMimeTypes.TryGetValue(ext, out var mimeTypes)
                && !mimeTypes.Contains(fileUpload.ContentType.ToLowerInvariant()))
            {
                TempData["Error"] = "Dinh dang file khong hop le. Vui long chon dung file .docx hoac .pdf.";
                return RedirectToAction("CreateExam", "Exam");
            }

            try
            {
                string raw = ext == ".docx" ? _fileParser.ExtractFromWord(fileUpload.OpenReadStream())
                    : ext == ".pdf" ? _fileParser.ExtractFromPdf(fileUpload.OpenReadStream())
                    : throw new Exception("Chi ho tro .docx va .pdf");

                HttpContext.Session.SetString("UploadedRawContent", _fileParser.FormatExamText(raw));
                return RedirectToAction("CreateExam", "Exam");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process uploaded exam file {FileName} for teacher {TeacherId}.",
                    fileUpload.FileName,
                    TeacherId);

                TempData["Error"] = "Khong the xu ly file luc nay. Vui long kiem tra dinh dang file va thu lai.";
                return RedirectToAction("CreateExam", "Exam");
            }
        }

        public async Task<IActionResult> ExportExamWord(int id)
        {
            if (!IsTeacher()) return RedirectToAction("Login", "Account");

            var fileBytes = await _examService.ExportExamToWordAsync(id, TeacherId);
            if (fileBytes == null)
            {
                TempData["Error"] = "Khong tim thay de thi hoac ban khong co quyen xuat file.";
                return RedirectToAction("Index");
            }

            var exam = await _examService.GetExamWithQuestionsAsync(id);
            var fileName = $"{exam?.Title ?? "DeThi"}_{id}.docx";

            return File(fileBytes,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                fileName);
        }

        private async Task PopulateAssignExamListsAsync()
        {
            ViewBag.ClassId = new SelectList(await _classService.GetClassesByTeacherAsync(TeacherId), "Id", "ClassName");
            var assignableExams = (await _examService.GetAssignableExamsForTeacherAsync(TeacherId))
                .Select(exam => new
                {
                    exam.Id,
                    Title = exam.CreatorId == TeacherId
                        ? exam.Title
                        : $"{exam.Title} (cong khai)"
                });

            ViewBag.ExamId = new SelectList(assignableExams, "Id", "Title");
        }
    }
}
