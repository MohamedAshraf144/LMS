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
            // Existing repositories...
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

            // Existing services...
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ICourseService, CourseService>();
            services.AddScoped<IEnrollmentService, EnrollmentService>();
            services.AddScoped<ILessonService, LessonService>();
            services.AddScoped<IAssignmentService, AssignmentService>();

            // Add Payment services
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<IPayMobService, PayMobService>();

            // Add HttpClient for PayMob
            services.AddHttpClient<IPayMobService, PayMobService>();

            return services;
        }
    }
}