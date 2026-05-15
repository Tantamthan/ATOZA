using ATOZA.Application.Abstractions.Persistence;
using ATOZA.Application.Abstractions.Services;
using ATOZA.Application.DTOs.Exam;
using ATOZA.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ATOZA.Infrastructure.Services
{
    public class ExamService : IExamService
    {
        private readonly IApplicationDbContext _db;

        public ExamService(IApplicationDbContext db) => _db = db;

        public async Task<int> CreateExamAsync(CreateExamDto dto, int creatorId)
        {
            var exam = new Exam
            {
                Title = dto.Title,
                Description = dto.Description,
                CreatorId = creatorId,
                DurationMinutes = dto.DurationMinutes,
                ExamMode = Enum.TryParse<ATOZA.Domain.Enums.ExamMode>(dto.ExamMode, true, out var mode)
                    ? mode
                    : ATOZA.Domain.Enums.ExamMode.Assessment,
                IsPublic = dto.IsPublic,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddMinutes(dto.DurationMinutes),
                CreatedAt = DateTime.UtcNow
            };

            _db.Exams.Add(exam);
            await _db.SaveChangesAsync();

            foreach (var q in dto.Questions)
            {
                _db.Questions.Add(new Question
                {
                    ExamId = exam.Id,
                    OrderNumber = q.OrderNumber,
                    Content = q.Content,
                    OptionA = q.OptionA,
                    OptionB = q.OptionB,
                    OptionC = q.OptionC,
                    OptionD = q.OptionD,
                    CorrectAnswer = q.CorrectAnswer
                });
            }

            if (dto.Questions.Count > 0)
                await _db.SaveChangesAsync();

            return exam.Id;
        }

        public async Task<Exam?> GetExamWithQuestionsAsync(int examId)
        {
            var exam = await _db.Exams
                .Include(e => e.Questions)
                .FirstOrDefaultAsync(e => e.Id == examId);

            if (exam != null)
                exam.Questions = exam.Questions.OrderBy(q => q.OrderNumber).ToList();

            return exam;
        }

        public async Task<StudentExamAccessResultDto> GetExamForStudentAsync(int examId, int studentId)
        {
            var assignment = await _db.ClassAssignments
                .Include(a => a.Exam)
                .ThenInclude(e => e.Questions)
                .Where(a => a.ExamId == examId)
                .Where(a => a.Class.ClassStudents.Any(cs => cs.StudentId == studentId))
                .OrderByDescending(a => a.AssignedAt)
                .FirstOrDefaultAsync();

            if (assignment == null)
            {
                return new StudentExamAccessResultDto
                {
                    Success = false,
                    Message = "Ban khong co quyen truy cap de thi nay."
                };
            }

            var now = DateTime.UtcNow;
            if (assignment.AvailableFrom > now)
            {
                return new StudentExamAccessResultDto
                {
                    Success = false,
                    Message = "Bai thi chua den thoi gian mo."
                };
            }

            if (assignment.DueDate < now)
            {
                return new StudentExamAccessResultDto
                {
                    Success = false,
                    Message = "Bai thi da qua han nop."
                };
            }

            if (await HasSubmittedAsync(examId, studentId))
            {
                return new StudentExamAccessResultDto
                {
                    Success = false,
                    Message = "Ban da nop bai thi nay roi."
                };
            }

            assignment.Exam.Questions = assignment.Exam.Questions
                .OrderBy(q => q.OrderNumber)
                .ToList();

            return new StudentExamAccessResultDto
            {
                Success = true,
                Exam = assignment.Exam
            };
        }

        public Task<bool> HasSubmittedAsync(int examId, int studentId)
        {
            return _db.Submissions.AnyAsync(s => s.ExamId == examId && s.StudentId == studentId);
        }

        public Task<List<Exam>> GetAllExamsAsync()
        {
            return _db.Exams.ToListAsync();
        }

        public Task<List<Exam>> GetExamsByCreatorAsync(int creatorId)
        {
            return _db.Exams
                .Where(e => e.CreatorId == creatorId)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        public Task<List<Exam>> GetAssignableExamsForTeacherAsync(int teacherId)
        {
            return _db.Exams
                .Include(e => e.Creator)
                .Where(e => e.CreatorId == teacherId || e.IsPublic)
                .OrderByDescending(e => e.CreatorId == teacherId)
                .ThenByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> SetExamVisibilityAsync(int examId, int teacherId, bool isPublic)
        {
            var exam = await _db.Exams.FirstOrDefaultAsync(e =>
                e.Id == examId && e.CreatorId == teacherId);

            if (exam == null)
                return false;

            exam.IsPublic = isPublic;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<PracticeAnswerResultDto> CheckPracticeAnswerAsync(CheckPracticeAnswerDto dto, int studentId)
        {
            var selectedOption = (dto.SelectedOption ?? string.Empty).Trim().ToUpperInvariant();
            if (!new[] { "A", "B", "C", "D" }.Contains(selectedOption))
            {
                return new PracticeAnswerResultDto
                {
                    Success = false,
                    Message = "Lua chon khong hop le."
                };
            }

            var access = await GetExamForStudentAsync(dto.ExamId, studentId);
            if (!access.Success || access.Exam == null)
            {
                return new PracticeAnswerResultDto
                {
                    Success = false,
                    Message = access.Message
                };
            }

            if (access.Exam.ExamMode != ATOZA.Domain.Enums.ExamMode.Practice)
            {
                return new PracticeAnswerResultDto
                {
                    Success = false,
                    Message = "De thi nay khong phai che do luyen tap."
                };
            }

            var question = access.Exam.Questions.FirstOrDefault(q => q.Id == dto.QuestionId);
            if (question == null)
            {
                return new PracticeAnswerResultDto
                {
                    Success = false,
                    Message = "Khong tim thay cau hoi."
                };
            }

            var correctAnswer = (question.CorrectAnswer ?? string.Empty).Trim().ToUpperInvariant();
            return new PracticeAnswerResultDto
            {
                Success = true,
                IsCorrect = selectedOption == correctAnswer,
                CorrectAnswer = correctAnswer,
                Message = selectedOption == correctAnswer ? "Chinh xac." : "Chua dung."
            };
        }

        public async Task<Exam?> GetExamForEditAsync(int examId, int teacherId)
        {
            return await _db.Exams
                .Include(e => e.Questions)
                .FirstOrDefaultAsync(e => e.Id == examId && e.CreatorId == teacherId);
        }

        public async Task<bool> UpdateExamAsync(UpdateExamDto dto, int teacherId)
        {
            var exam = await _db.Exams
                .Include(e => e.Questions)
                .FirstOrDefaultAsync(e => e.Id == dto.ExamId && e.CreatorId == teacherId);

            if (exam == null)
                return false;

            // Cập nhật metadata
            exam.Title = dto.Title;
            exam.Description = dto.Description;
            exam.DurationMinutes = dto.DurationMinutes;
            exam.IsPublic = dto.IsPublic;

            if (Enum.TryParse<ATOZA.Domain.Enums.ExamMode>(dto.ExamMode, true, out var mode))
                exam.ExamMode = mode;

            // Lấy danh sách QuestionId cũ để xóa SubmissionDetail liên quan
            var oldQuestionIds = exam.Questions.Select(q => q.Id).ToList();

            if (oldQuestionIds.Count > 0)
            {
                // Xóa SubmissionDetail tham chiếu đến các câu hỏi cũ (FK Restrict)
                var relatedDetails = await _db.SubmissionDetails
                    .Where(sd => oldQuestionIds.Contains(sd.QuestionId))
                    .ToListAsync();

                if (relatedDetails.Count > 0)
                    _db.SubmissionDetails.RemoveRange(relatedDetails);

                // Xóa Submission rỗng (không còn detail nào) thuộc đề thi này
                var relatedSubmissions = await _db.Submissions
                    .Include(s => s.SubmissionDetails)
                    .Where(s => s.ExamId == exam.Id)
                    .ToListAsync();

                var emptySubmissions = relatedSubmissions
                    .Where(s => s.SubmissionDetails.All(sd => oldQuestionIds.Contains(sd.QuestionId)))
                    .ToList();

                if (emptySubmissions.Count > 0)
                    _db.Submissions.RemoveRange(emptySubmissions);
            }

            // Xóa câu hỏi cũ
            _db.Questions.RemoveRange(exam.Questions);

            // Thêm câu hỏi mới
            foreach (var q in dto.Questions)
            {
                _db.Questions.Add(new Question
                {
                    ExamId = exam.Id,
                    OrderNumber = q.OrderNumber,
                    Content = q.Content,
                    OptionA = q.OptionA,
                    OptionB = q.OptionB,
                    OptionC = q.OptionC,
                    OptionD = q.OptionD,
                    CorrectAnswer = q.CorrectAnswer
                });
            }

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<byte[]?> ExportExamToWordAsync(int examId, int teacherId)
        {
            var exam = await _db.Exams
                .Include(e => e.Questions)
                .FirstOrDefaultAsync(e => e.Id == examId && e.CreatorId == teacherId);

            if (exam == null)
                return null;

            var sortedQuestions = exam.Questions.OrderBy(q => q.OrderNumber).ToList();

            using var ms = new MemoryStream();
            using (var wordDoc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Create(
                ms, DocumentFormat.OpenXml.WordprocessingDocumentType.Document, true))
            {
                var mainPart = wordDoc.AddMainDocumentPart();
                mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
                var body = mainPart.Document.AppendChild(
                    new DocumentFormat.OpenXml.Wordprocessing.Body());

                // --- Tiêu đề đề thi ---
                body.AppendChild(CreateParagraph(exam.Title, bold: true, fontSize: 32,
                    alignment: DocumentFormat.OpenXml.Wordprocessing.JustificationValues.Center));

                // --- Thông tin phụ ---
                body.AppendChild(CreateParagraph(
                    $"Thoi gian: {exam.DurationMinutes} phut  |  So cau: {sortedQuestions.Count}",
                    fontSize: 22,
                    alignment: DocumentFormat.OpenXml.Wordprocessing.JustificationValues.Center));

                // --- Dòng trống ---
                body.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Paragraph());

                // --- Câu hỏi ---
                for (int i = 0; i < sortedQuestions.Count; i++)
                {
                    var q = sortedQuestions[i];

                    // Câu hỏi: "Cau 1: Nội dung..."
                    body.AppendChild(CreateQuestionParagraph(i + 1, q.Content));

                    // Các đáp án
                    AppendOption(body, "A", q.OptionA, q.CorrectAnswer == "A");
                    AppendOption(body, "B", q.OptionB, q.CorrectAnswer == "B");
                    AppendOption(body, "C", q.OptionC, q.CorrectAnswer == "C");
                    AppendOption(body, "D", q.OptionD, q.CorrectAnswer == "D");

                    // Dòng trống giữa các câu
                    body.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Paragraph());
                }

                // --- Đáp án cuối trang ---
                body.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Paragraph());
                body.AppendChild(CreateParagraph("--- DAP AN ---", bold: true, fontSize: 26,
                    alignment: DocumentFormat.OpenXml.Wordprocessing.JustificationValues.Center));

                for (int i = 0; i < sortedQuestions.Count; i++)
                {
                    var q = sortedQuestions[i];
                    body.AppendChild(CreateParagraph($"Cau {i + 1}: {q.CorrectAnswer}", fontSize: 22));
                }

                mainPart.Document.Save();
            }

            return ms.ToArray();
        }

        // --- Helper methods cho xuất Word ---

        private static DocumentFormat.OpenXml.Wordprocessing.Paragraph CreateParagraph(
            string text, bool bold = false, int fontSize = 24,
            DocumentFormat.OpenXml.Wordprocessing.JustificationValues? alignment = null)
        {
            var run = new DocumentFormat.OpenXml.Wordprocessing.Run();
            var runProps = new DocumentFormat.OpenXml.Wordprocessing.RunProperties();
            runProps.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.FontSize
            {
                Val = fontSize.ToString()
            });
            if (bold)
                runProps.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Bold());

            run.PrependChild(runProps);
            run.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(text)
            {
                Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve
            });

            var para = new DocumentFormat.OpenXml.Wordprocessing.Paragraph();
            if (alignment.HasValue)
            {
                var pProps = new DocumentFormat.OpenXml.Wordprocessing.ParagraphProperties();
                pProps.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Justification
                {
                    Val = alignment.Value
                });
                para.PrependChild(pProps);
            }

            para.AppendChild(run);
            return para;
        }

        private static DocumentFormat.OpenXml.Wordprocessing.Paragraph CreateQuestionParagraph(
            int number, string content)
        {
            var para = new DocumentFormat.OpenXml.Wordprocessing.Paragraph();

            // Bold "Cau X:"
            var labelRun = new DocumentFormat.OpenXml.Wordprocessing.Run();
            var labelProps = new DocumentFormat.OpenXml.Wordprocessing.RunProperties();
            labelProps.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Bold());
            labelProps.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.FontSize { Val = "24" });
            labelRun.PrependChild(labelProps);
            labelRun.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text($"Cau {number}: ")
            {
                Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve
            });

            // Nội dung bình thường
            var contentRun = new DocumentFormat.OpenXml.Wordprocessing.Run();
            var contentProps = new DocumentFormat.OpenXml.Wordprocessing.RunProperties();
            contentProps.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.FontSize { Val = "24" });
            contentRun.PrependChild(contentProps);
            contentRun.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(content)
            {
                Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve
            });

            para.AppendChild(labelRun);
            para.AppendChild(contentRun);
            return para;
        }

        private static void AppendOption(
            DocumentFormat.OpenXml.Wordprocessing.Body body,
            string key, string content, bool isCorrect)
        {
            var para = new DocumentFormat.OpenXml.Wordprocessing.Paragraph();

            // Thụt lề cho đáp án
            var pProps = new DocumentFormat.OpenXml.Wordprocessing.ParagraphProperties();
            pProps.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Indentation
            {
                Left = "720" // ~0.5 inch
            });
            para.PrependChild(pProps);

            var run = new DocumentFormat.OpenXml.Wordprocessing.Run();
            var runProps = new DocumentFormat.OpenXml.Wordprocessing.RunProperties();
            runProps.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.FontSize { Val = "24" });

            if (isCorrect)
            {
                runProps.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Bold());
                runProps.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Color { Val = "FF0000" });
            }

            run.PrependChild(runProps);

            var marker = isCorrect ? "*" : "";
            run.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text($"{marker}{key}. {content}")
            {
                Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve
            });

            para.AppendChild(run);
            body.AppendChild(para);
        }
    }
}
