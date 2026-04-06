using ATOZA.Application.Abstractions.Persistence;
using ATOZA.Domain.Entities;
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User
            modelBuilder.Entity<User>(e => {
                e.ToTable("Users");
                e.HasKey(u => u.Id);
                e.Property(u => u.Email).IsRequired().HasMaxLength(200);
                e.Property(u => u.UserName).IsRequired().HasMaxLength(100);
                e.HasIndex(u => u.Email).IsUnique();
                e.HasIndex(u => u.UserName).IsUnique();
            });

            // Exam
            modelBuilder.Entity<Exam>(e => {
                e.ToTable("Exams");
                e.HasKey(x => x.Id);
                e.HasOne(x => x.Creator).WithMany(u => u.Exams).HasForeignKey(x => x.CreatorId);
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
                e.HasOne(s => s.Exam).WithMany(x => x.Submissions).HasForeignKey(s => s.ExamId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(s => s.Student).WithMany(u => u.Submissions).HasForeignKey(s => s.StudentId).OnDelete(DeleteBehavior.Restrict);
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
