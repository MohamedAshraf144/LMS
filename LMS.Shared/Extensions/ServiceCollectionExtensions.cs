// LMS.Shared/Extensions/ServiceCollectionExtensions.cs
using Microsoft.Extensions.DependencyInjection;
using LMS.Core.Interfaces;
using LMS.Core.Interfaces.Services;
using LMS.Infrastructure.Repositories;
using LMS.Application.Services;

namespace LMS.Shared.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddLMSServices(this IServiceCollection services)
        {
            // Repositories
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

            // Services
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ICourseService, CourseService>();
            services.AddScoped<IEnrollmentService, EnrollmentService>();
            services.AddScoped<ILessonService, LessonService>();
            services.AddScoped<IAssignmentService, AssignmentService>();

            return services;
        }
    }
}