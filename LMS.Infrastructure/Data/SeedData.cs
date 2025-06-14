using LMS.Core.Enums;
using LMS.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Infrastructure.Data
{
    public static class SeedData
    {
        public static void Initialize(ApplicationDbContext context)
        {
            if (context.Users.Any())
                return; // DB has been seeded

            // Seed Categories
            var categories = new[]
            {
                new Category { Name = "Programming", Description = "Software development and programming", IsActive = true },
                new Category { Name = "Design", Description = "Graphic design and UI/UX", IsActive = true },
                new Category { Name = "Business", Description = "Business and entrepreneurship", IsActive = true },
                new Category { Name = "Marketing", Description = "Digital marketing", IsActive = true },
                new Category { Name = "Data Science", Description = "Data analysis and AI", IsActive = true }
            };

            context.Categories.AddRange(categories);
            context.SaveChanges();

            // Seed Admin User
            var adminUser = new User
            {
                FirstName = "Admin",
                LastName = "User",
                Email = "admin@lms.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Role = UserRole.Admin,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            context.Users.Add(adminUser);
            context.SaveChanges();

            // Seed Instructor
            var instructor = new User
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "instructor@lms.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("instructor123"),
                Role = UserRole.Instructor,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            context.Users.Add(instructor);
            context.SaveChanges();

            // Seed Sample Course
            var course = new Course
            {
                Title = "Introduction to Web Development",
                Description = "Learn the basics of HTML, CSS, and JavaScript to build modern websites.",
                InstructorId = instructor.Id,
                CategoryId = categories[0].Id,
                Price = 49.99m,
                Level = CourseLevel.Beginner,
                Status = CourseStatus.Published,
                Duration = 40,
                CreatedDate = DateTime.UtcNow,
                PublishedDate = DateTime.UtcNow
            };

            context.Courses.Add(course);
            context.SaveChanges();

            // Seed Lessons
            var lessons = new[]
            {
                new Lesson
                {
                    Title = "Introduction to HTML",
                    Description = "Learn the basic structure of HTML documents",
                    Content = "HTML is the foundation of web development...",
                    Duration = 30,
                    OrderIndex = 1,
                    CourseId = course.Id,
                    IsPublished = true,
                    CreatedDate = DateTime.UtcNow
                },
                new Lesson
                {
                    Title = "CSS Fundamentals",
                    Description = "Style your web pages with CSS",
                    Content = "CSS allows you to style HTML elements...",
                    Duration = 45,
                    OrderIndex = 2,
                    CourseId = course.Id,
                    IsPublished = true,
                    CreatedDate = DateTime.UtcNow
                },
                new Lesson
                {
                    Title = "JavaScript Basics",
                    Description = "Add interactivity with JavaScript",
                    Content = "JavaScript brings your web pages to life...",
                    Duration = 60,
                    OrderIndex = 3,
                    CourseId = course.Id,
                    IsPublished = true,
                    CreatedDate = DateTime.UtcNow
                }
            };

            context.Lessons.AddRange(lessons);
            context.SaveChanges();
        }
    }
}
