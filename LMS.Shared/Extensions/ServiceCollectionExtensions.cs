// LMS.Shared/Extensions/ServiceCollectionExtensions.cs - محدث لاستخدام Mock Service
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

            // استخدام Mock PayMob Service بدلاً من الحقيقي
            services.AddScoped<IPayMobService, MockPayMobService>();

            // إذا كنت تريد استخدام PayMob الحقيقي، استخدم هذا السطر بدلاً من السطر السابق:
            // services.AddScoped<IPayMobService, PayMobService>();

            // Add HttpClient for PayMob (حتى لو كان mock)
            services.AddHttpClient<IPayMobService, MockPayMobService>();

            return services;
        }
    }
}