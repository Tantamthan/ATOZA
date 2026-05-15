using ATOZA.Application.Abstractions.Persistence;
using ATOZA.Domain.Entities;
using ATOZA.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace ATOZA.Infrastructure.Persistence
{
    public class ATOZADbContext : DbContext, IApplicationDbContext
    {
        public ATOZADbContext(DbContextOptions<ATOZADbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Exam> Exams => Set<Exam>();
        public DbSet<Question> Questions => Set<Question>();
        public DbSet<Class> Classes => Set<Class>();
        public DbSet<ClassStudent> ClassStudents => Set<ClassStudent>();
        public DbSet<ClassAssignment> ClassAssignments => Set<ClassAssignment>();
        public DbSet<Submission> Submissions => Set<Submission>();
        public DbSet<SubmissionDetail> SubmissionDetails => Set<SubmissionDetail>();
        public DbSet<ExamAttempt> ExamAttempts => Set<ExamAttempt>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User
            modelBuilder.Entity<User>(e => {
                e.ToTable("Users");
                e.HasKey(u => u.Id);
                e.Property(u => u.Email).IsRequired().HasMaxLength(200);
                e.Property(u => u.UserName).IsRequired().HasMaxLength(100);
                e.Property(u => u.IsActive).HasDefaultValue(true);
                e.HasIndex(u => u.Email).IsUnique();
                e.HasIndex(u => u.UserName).IsUnique();
                e.HasData(new User
                {
                    Id = -1,
                    FullName = "System Admin",
                    Email = "admin@atoza.vn",
                    UserName = "admin",
                    PasswordHash = "PBKDF2$100000$39Xdq+NQ2Yt9iv9N838zgw==$+JZYl/jpXSEclHFp1LCWzWwVSMI7gqNtQqX3045FutE=",
                    Role = UserRole.Admin,
                    IsActive = true,
                    ApprovalStatus = ApprovalStatus.Approved,
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                });
            });

            // Exam
            modelBuilder.Entity<Exam>(e => {
                e.ToTable("Exams");
                e.HasKey(x => x.Id);
                e.Property(x => x.IsPublic).HasDefaultValue(false);
                e.Property(x => x.VersionNumber).HasDefaultValue(1);
                e.Property(x => x.IsArchived).HasDefaultValue(false);
                e.HasOne(x => x.Creator).WithMany(u => u.Exams).HasForeignKey(x => x.CreatorId);
                e.HasOne(x => x.ParentExam).WithMany(x => x.Versions).HasForeignKey(x => x.ParentExamId).OnDelete(DeleteBehavior.Restrict);
            });

            // Question
            modelBuilder.Entity<Question>(e => {
                e.ToTable("Questions");
                e.HasKey(q => q.Id);
                e.HasOne(q => q.Exam).WithMany(x => x.Questions).HasForeignKey(q => q.ExamId);
            });

            // Class
            modelBuilder.Entity<Class>(e => {
                e.ToTable("Classes");
                e.HasKey(c => c.Id);
                e.HasOne(c => c.Teacher).WithMany(u => u.Classes).HasForeignKey(c => c.TeacherId);
                e.HasIndex(c => c.JoinCode).IsUnique();
            });

            // ClassStudent (composite key)
            modelBuilder.Entity<ClassStudent>(e => {
                e.ToTable("ClassStudents");
                e.HasKey(cs => new { cs.ClassId, cs.StudentId });
                e.HasOne(cs => cs.Class).WithMany(c => c.ClassStudents).HasForeignKey(cs => cs.ClassId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(cs => cs.Student).WithMany(u => u.ClassStudents).HasForeignKey(cs => cs.StudentId).OnDelete(DeleteBehavior.Restrict);
            });

            // ClassAssignment
            modelBuilder.Entity<ClassAssignment>(e => {
                e.ToTable("ClassAssignments");
                e.HasKey(ca => ca.Id);
                e.HasOne(ca => ca.Class).WithMany(c => c.ClassAssignments).HasForeignKey(ca => ca.ClassId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(ca => ca.Exam).WithMany(x => x.ClassAssignments).HasForeignKey(ca => ca.ExamId).OnDelete(DeleteBehavior.Restrict);
            });

            // Submission
            modelBuilder.Entity<Submission>(e => {
                e.ToTable("Submissions");
                e.HasKey(s => s.Id);
                e.HasIndex(s => new { s.ExamId, s.StudentId }).IsUnique();
                e.HasOne(s => s.Exam).WithMany(x => x.Submissions).HasForeignKey(s => s.ExamId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(s => s.Student).WithMany(u => u.Submissions).HasForeignKey(s => s.StudentId).OnDelete(DeleteBehavior.Restrict);
            });

            // ExamAttempt
            modelBuilder.Entity<ExamAttempt>(e => {
                e.ToTable("ExamAttempts");
                e.HasKey(a => a.Id);
                e.HasIndex(a => new { a.ExamId, a.StudentId, a.Status });
                e.HasOne(a => a.Exam).WithMany(x => x.ExamAttempts).HasForeignKey(a => a.ExamId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(a => a.Student).WithMany(u => u.ExamAttempts).HasForeignKey(a => a.StudentId).OnDelete(DeleteBehavior.Restrict);
            });

            // SubmissionDetail
            modelBuilder.Entity<SubmissionDetail>(e => {
                e.ToTable("SubmissionDetails");
                e.HasKey(sd => sd.Id);
                e.HasOne(sd => sd.Submission).WithMany(s => s.SubmissionDetails).HasForeignKey(sd => sd.SubmissionId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(sd => sd.Question).WithMany(q => q.SubmissionDetails).HasForeignKey(sd => sd.QuestionId).OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
