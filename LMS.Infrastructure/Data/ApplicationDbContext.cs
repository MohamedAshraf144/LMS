// LMS.Infrastructure/Data/ApplicationDbContext.cs
using Microsoft.EntityFrameworkCore;
using LMS.Core.Models;

namespace LMS.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<AssignmentSubmission> AssignmentSubmissions { get; set; }
        public DbSet<LessonProgress> LessonProgresses { get; set; }
        public DbSet<Review> Reviews { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User Configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PasswordHash).IsRequired();
            });

            // Category Configuration
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            });

            // Course Configuration
            modelBuilder.Entity<Course>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.Instructor)
                    .WithMany(u => u.CreatedCourses)
                    .HasForeignKey(e => e.InstructorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Category)
                    .WithMany(c => c.Courses)
                    .HasForeignKey(e => e.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Lesson Configuration
            modelBuilder.Entity<Lesson>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);

                entity.HasOne(e => e.Course)
                    .WithMany(c => c.Lessons)
                    .HasForeignKey(e => e.CourseId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Enrollment Configuration
            modelBuilder.Entity<Enrollment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.UserId, e.CourseId }).IsUnique();
                entity.Property(e => e.Progress).HasColumnType("decimal(5,2)");
                entity.Property(e => e.Grade).HasColumnType("decimal(5,2)");

                entity.HasOne(e => e.User)
                    .WithMany(u => u.Enrollments)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Course)
                    .WithMany(c => c.Enrollments)
                    .HasForeignKey(e => e.CourseId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Assignment Configuration
            modelBuilder.Entity<Assignment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);

                entity.HasOne(e => e.Course)
                    .WithMany(c => c.Assignments)
                    .HasForeignKey(e => e.CourseId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.CreatedBy)
                    .WithMany(u => u.Assignments)
                    .HasForeignKey(e => e.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // AssignmentSubmission Configuration - هنا المشكلة!
            modelBuilder.Entity<AssignmentSubmission>(entity =>
            {
                entity.HasKey(e => e.Id);

                // علاقة مع Assignment
                entity.HasOne(e => e.Assignment)
                    .WithMany(a => a.Submissions)
                    .HasForeignKey(e => e.AssignmentId)
                    .OnDelete(DeleteBehavior.Cascade);

                // علاقة مع User (اللي عمل submit)
                entity.HasOne(e => e.User)
                    .WithMany(u => u.AssignmentSubmissions)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // علاقة مع User (اللي عمل grading) - هنا المشكلة!
                entity.HasOne(e => e.GradedBy)
                    .WithMany() // مافيش navigation property في User للـ graded submissions
                    .HasForeignKey(e => e.GradedByUserId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false); // لأن ممكن يكون null
            });

            // LessonProgress Configuration
            modelBuilder.Entity<LessonProgress>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.EnrollmentId, e.LessonId }).IsUnique();

                entity.HasOne(e => e.Enrollment)
                    .WithMany(en => en.LessonProgresses)
                    .HasForeignKey(e => e.EnrollmentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Lesson)
                    .WithMany(l => l.LessonProgresses)
                    .HasForeignKey(e => e.LessonId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Review Configuration
            modelBuilder.Entity<Review>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.UserId, e.CourseId }).IsUnique();
                entity.Property(e => e.Rating).IsRequired();

                entity.HasOne(e => e.User)
                    .WithMany() // مافيش navigation property في User للـ reviews
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Course)
                    .WithMany(c => c.Reviews)
                    .HasForeignKey(e => e.CourseId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}