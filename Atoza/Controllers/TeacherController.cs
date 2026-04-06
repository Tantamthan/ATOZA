using ATOZA.Application.Abstractions.Services;
using ATOZA.Application.DTOs.Class;
using ATOZA.Application.DTOs.Submission;
using ATOZA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Atoza_Web.Controllers
{
    public class TeacherController : Controller
    {
        private readonly IClassService _classService;
        private readonly IExamService _examService;
        private readonly ISubmissionService _submissionService;
        private readonly IFileParserService _fileParser;

        public TeacherController(
            IClassService classService,
            IExamService examService,
            ISubmissionService submissionService,
            IFileParserService fileParser)
        {
            _classService = classService;
            _examService = examService;
            _submissionService = submissionService;
            _fileParser = fileParser;
        }

        private bool IsTeacher() =>
            HttpContext.Session.GetString("Role") == "Teacher";

        private int TeacherId =>
            HttpContext.Session.GetInt32("IdUser") ?? 0;

        // =====================================================
        // DASHBOARD
        // =====================================================

        public async Task<IActionResult> Index()
        {
            if (!IsTeacher()) return RedirectToAction("Login", "Account");
            var exams = await _examService.GetExamsByCreatorAsync(TeacherId);
            ViewBag.ClassCount = (await _classService.GetClassesByTeacherAsync(TeacherId)).Count;
            return View(exams);
        }

        // =====================================================
        // QUẢN LÝ LỚP HỌC
        // =====================================================

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
            if (cls == null) { TempData["Error"] = "Không tìm thấy lớp học."; return RedirectToAction("ClassList"); }
            return View(cls);
        }

        public async Task<IActionResult> ExportStudents(int classId)
        {
            if (!IsTeacher()) return RedirectToAction("Login", "Account");
            var data = await _classService.ExportStudentsCsvAsync(classId, TeacherId);
            if (data == null) return RedirectToAction("ClassList");
            return File(data, "text/csv", $"DanhSachHocSinh_{classId}.csv");
        }

        // =====================================================
        // GIAO BÀI TẬP
        // =====================================================

        public async Task<IActionResult> AssignExam()
        {
            if (!IsTeacher()) return RedirectToAction("Login", "Account");
            ViewBag.ClassId = new SelectList(await _classService.GetClassesByTeacherAsync(TeacherId), "Id", "ClassName");
            ViewBag.ExamId = new SelectList(await _examService.GetAllExamsAsync(), "Id", "Title");
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignExam(AssignExamDto dto)
        {
            if (!IsTeacher()) return RedirectToAction("Login", "Account");
            if (ModelState.IsValid)
            {
                bool ok = _classService.AssignExamAsync(dto, out string? error).Result;
                if (ok) { TempData["Success"] = "Giao bài thành công!"; return RedirectToAction("Index"); }
                ModelState.AddModelError("", error ?? "Lỗi không xác định");
            }
            ViewBag.ClassId = new SelectList(await _classService.GetClassesByTeacherAsync(TeacherId), "Id", "ClassName");
            ViewBag.ExamId = new SelectList(await _examService.GetAllExamsAsync(), "Id", "Title");
            return View(dto);
        }

        public async Task<IActionResult> ClassAssignmentsList(int classId)
        {
            if (!IsTeacher()) return RedirectToAction("Login", "Account");
            var cls = await _classService.GetClassDetailAsync(classId, TeacherId);
            if (cls == null) { TempData["Error"] = "Không tìm thấy lớp."; return RedirectToAction("ClassList"); }
            ViewBag.ClassName = cls.ClassName;
            ViewBag.ClassId = classId;
            return View(await _classService.GetClassAssignmentsAsync(classId, TeacherId));
        }

        // =====================================================
        // BÁO CÁO KẾT QUẢ
        // =====================================================

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

        // =====================================================
        // UPLOAD ĐỀ THI TỪ FILE
        // =====================================================

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult ProcessExamFile(IFormFile fileUpload)
        {
            if (!IsTeacher()) return RedirectToAction("Login", "Account");
            if (fileUpload == null || fileUpload.Length == 0)
            { TempData["Error"] = "Vui lòng chọn file."; return RedirectToAction("CreateExam", "Exam"); }

            string ext = Path.GetExtension(fileUpload.FileName).ToLower();
            try
            {
                string raw = ext == ".docx" ? _fileParser.ExtractFromWord(fileUpload.OpenReadStream())
                           : ext == ".pdf" ? _fileParser.ExtractFromPdf(fileUpload.OpenReadStream())
                           : throw new Exception("Chỉ hỗ trợ .docx và .pdf");

                TempData["RawContent"] = _fileParser.FormatExamText(raw);
                return RedirectToAction("CreateExam", "Exam");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi: " + ex.Message;
                return RedirectToAction("CreateExam", "Exam");
            }
        }
    }
}
