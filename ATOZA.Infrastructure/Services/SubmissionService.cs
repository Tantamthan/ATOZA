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
            // Chống nộp lại
            if (_db.Submissions.Any(s => s.ExamId == dto.ExamId && s.StudentId == studentId))
                return new SubmitResultDto { Success = false, Message = "Bạn đã nộp bài này rồi!" };

            var exam = _db.Exams
                          .Include(e => e.Questions)
                          .FirstOrDefault(e => e.Id == dto.ExamId);

            if (exam == null)
                return new SubmitResultDto { Success = false, Message = "Không tìm thấy đề thi." };

            // Chấm điểm
            int correct = 0;
            var details = new List<SubmissionDetail>();

            foreach (var ans in dto.Answers)
            {
                var q = exam.Questions.FirstOrDefault(x => x.Id == ans.QuestionId);
                bool isCorrect = q != null &&
                    q.CorrectAnswer.Trim().ToUpper() == ans.SelectedOption?.Trim().ToUpper();
                if (isCorrect) correct++;

                details.Add(new SubmissionDetail
                {
                    QuestionId = ans.QuestionId,
                    Answer = ans.SelectedOption,
                    IsCorrect = isCorrect
                });
            }

            double score = exam.Questions.Count > 0
                ? Math.Round((double)correct / exam.Questions.Count * 10, 2) : 0;

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

            return new SubmitResultDto { Success = true, Score = score, Message = $"Điểm: {score}" };
        }

        public Task<List<Submission>> GetStudentSubmissionsAsync(int studentId)
        {
            return Task.FromResult(_db.Submissions
                .Include(s => s.Exam)
                .Where(s => s.StudentId == studentId)
                .OrderByDescending(s => s.SubmitTime)
                .ToList());
        }

        public Task<Submission?> GetSubmissionDetailAsync(int examId, int studentId)
        {
            return Task.FromResult(_db.Submissions
                .Include(s => s.SubmissionDetails)
                .Include("Exam.Questions")
                .FirstOrDefault(s => s.ExamId == examId && s.StudentId == studentId));
        }

        public Task<List<StudentReportDto>> GetSubmissionReportAsync(int classId, int examId)
        {
            var students = _db.ClassStudents
                .Where(cs => cs.ClassId == classId)
                .Select(cs => cs.Student).ToList();

            var results = _db.Submissions
                .Where(r => r.ExamId == examId).ToList();

            var report = students.Select(s =>
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

            return Task.FromResult(report);
        }
    }
}
