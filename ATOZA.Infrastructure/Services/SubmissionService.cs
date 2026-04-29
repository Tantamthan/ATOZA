using ATOZA.Application.Abstractions.Persistence;
using ATOZA.Application.Abstractions.Services;
using ATOZA.Application.DTOs.Submission;
using ATOZA.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ATOZA.Infrastructure.Services
{
    public class SubmissionService : ISubmissionService
    {
        private readonly IApplicationDbContext _db;

        public SubmissionService(IApplicationDbContext db) => _db = db;

        public async Task<SubmitResultDto> SubmitExamAsync(SubmitExamDto dto, int studentId)
        {
            var assignment = await GetOpenAssignmentForStudentAsync(dto.ExamId, studentId);
            if (assignment == null)
            {
                return new SubmitResultDto
                {
                    Success = false,
                    Message = "Ban khong co quyen nop bai thi nay hoac bai thi khong trong thoi gian mo."
                };
            }

            if (await _db.Submissions.AnyAsync(s => s.ExamId == dto.ExamId && s.StudentId == studentId))
                return new SubmitResultDto { Success = false, Message = "Ban da nop bai nay roi!" };

            var exam = assignment.Exam;
            var questionIds = exam.Questions.Select(q => q.Id).ToHashSet();
            if (dto.Answers.Any(a => !questionIds.Contains(a.QuestionId)))
            {
                return new SubmitResultDto
                {
                    Success = false,
                    Message = "Du lieu cau tra loi khong hop le."
                };
            }

            int correct = 0;
            var details = new List<SubmissionDetail>();

            foreach (var ans in dto.Answers)
            {
                var question = exam.Questions.First(q => q.Id == ans.QuestionId);
                bool isCorrect = question.CorrectAnswer.Trim().ToUpperInvariant()
                    == ans.SelectedOption?.Trim().ToUpperInvariant();

                if (isCorrect) correct++;

                details.Add(new SubmissionDetail
                {
                    QuestionId = ans.QuestionId,
                    Answer = ans.SelectedOption,
                    IsCorrect = isCorrect
                });
            }

            double score = exam.Questions.Count > 0
                ? Math.Round((double)correct / exam.Questions.Count * 10, 2)
                : 0;

            var submission = new Submission
            {
                ExamId = dto.ExamId,
                StudentId = studentId,
                Score = score,
                SubmitTime = DateTime.UtcNow
            };

            _db.Submissions.Add(submission);
            await _db.SaveChangesAsync();

            details.ForEach(d => d.SubmissionId = submission.Id);
            _db.SubmissionDetails.AddRange(details);
            await _db.SaveChangesAsync();

            return new SubmitResultDto { Success = true, Score = score, Message = $"Diem: {score}" };
        }

        public Task<List<Submission>> GetStudentSubmissionsAsync(int studentId)
        {
            return _db.Submissions
                .Include(s => s.Exam)
                .Where(s => s.StudentId == studentId)
                .OrderByDescending(s => s.SubmitTime)
                .ToListAsync();
        }

        public Task<Submission?> GetSubmissionDetailAsync(int examId, int studentId)
        {
            return _db.Submissions
                .Include(s => s.SubmissionDetails)
                .Include("Exam.Questions")
                .FirstOrDefaultAsync(s => s.ExamId == examId && s.StudentId == studentId);
        }

        public async Task<List<StudentReportDto>> GetSubmissionReportAsync(int classId, int examId)
        {
            bool isAssignedToClass = await _db.ClassAssignments
                .AnyAsync(a => a.ClassId == classId && a.ExamId == examId);

            if (!isAssignedToClass)
                return new List<StudentReportDto>();

            var students = await _db.ClassStudents
                .Where(cs => cs.ClassId == classId)
                .Select(cs => cs.Student)
                .ToListAsync();

            var results = await _db.Submissions
                .Where(r => r.ExamId == examId)
                .ToListAsync();

            return students.Select(s =>
            {
                var r = results.FirstOrDefault(x => x.StudentId == s.Id);
                return new StudentReportDto
                {
                    StudentId = s.Id,
                    StudentName = s.FullName,
                    Email = s.Email,
                    IsSubmitted = r != null,
                    Score = r?.Score,
                    FinishedAt = r?.SubmitTime
                };
            }).OrderByDescending(x => x.IsSubmitted).ThenBy(x => x.StudentName).ToList();
        }

        private Task<ClassAssignment?> GetOpenAssignmentForStudentAsync(int examId, int studentId)
        {
            var now = DateTime.UtcNow;

            return _db.ClassAssignments
                .Include(a => a.Exam)
                .ThenInclude(e => e.Questions)
                .Where(a => a.ExamId == examId)
                .Where(a => a.AvailableFrom <= now && a.DueDate >= now)
                .Where(a => a.Class.ClassStudents.Any(cs => cs.StudentId == studentId))
                .OrderByDescending(a => a.AssignedAt)
                .FirstOrDefaultAsync();
        }
    }
}
