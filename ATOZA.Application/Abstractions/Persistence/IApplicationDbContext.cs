using ATOZA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace ATOZA.Application.Abstractions.Persistence
{
    /// <summary>
    /// Contract cho DbContext – Application chỉ biết interface này, không biết EF
    /// </summary>
    public interface IApplicationDbContext
    {
        DbSet<User> Users { get; }
        DbSet<Exam> Exams { get; }
        DbSet<Question> Questions { get; }
        DbSet<Class> Classes { get; }
        DbSet<ClassStudent> ClassStudents { get; }
        DbSet<ClassAssignment> ClassAssignments { get; }
        DbSet<Submission> Submissions { get; }
        DbSet<SubmissionDetail> SubmissionDetails { get; }
        DbSet<ExamAttempt> ExamAttempts { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
