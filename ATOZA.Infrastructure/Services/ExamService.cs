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
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddMinutes(dto.DurationMinutes),
                CreatedAt = DateTime.UtcNow
            };

            _db.Exams.Add(exam);
            await _db.SaveChangesAsync();

            if (dto.Questions.Any())
            {
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
            }

            return exam.Id;
        }

        public Task<Exam?> GetExamWithQuestionsAsync(int examId)
        {
            var exam = _db.Exams
                          .Include(e => e.Questions)
                          .FirstOrDefault(e => e.Id == examId);

            if (exam != null)
                exam.Questions = exam.Questions.OrderBy(q => q.OrderNumber).ToList();

            return Task.FromResult(exam);
        }

        public Task<bool> HasSubmittedAsync(int examId, int studentId)
        {
            return Task.FromResult(
                _db.Submissions.Any(s => s.ExamId == examId && s.StudentId == studentId));
        }

        public Task<List<Exam>> GetAllExamsAsync()
        {
            return Task.FromResult(_db.Exams.ToList());
        }

        public Task<List<Exam>> GetExamsByCreatorAsync(int creatorId)
        {
            return Task.FromResult(
                _db.Exams
                   .Where(e => e.CreatorId == creatorId)
                   .OrderByDescending(e => e.CreatedAt)
                   .ToList());
        }
    }
}
