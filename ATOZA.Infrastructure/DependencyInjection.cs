using ATOZA.Application.Abstractions.Persistence;
using ATOZA.Application.Abstractions.Services;
using ATOZA.Infrastructure.Persistence;
using ATOZA.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ATOZA.Infrastructure
{
    /// <summary>
    /// Đăng ký tất cả services của tầng Infrastructure vào DI container
    /// Gọi trong Program.cs: builder.Services.AddInfrastructure(configuration);
    /// </summary>
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // 1. Đăng ký DbContext
            services.AddDbContext<ATOZADbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"), b => b.MigrationsAssembly("ATOZA.Infrastructure")));


            // 2. Gán IApplicationDbContext → ATOZADbContext
            services.AddScoped<IApplicationDbContext>(
                provider => provider.GetRequiredService<ATOZADbContext>());

            // 3. Đăng ký các Service implementations
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IExamService, ExamService>();
            services.AddScoped<IClassService, ClassService>();
            services.AddScoped<ISubmissionService, SubmissionService>();
            services.AddScoped<IExamAttemptService, ExamAttemptService>();
            services.AddScoped<IFileParserService, FileParserService>();
            services.AddScoped<IAdminService, AdminService>();
            services.AddScoped<IEmailSender, SmtpEmailSender>();

            return services;
        }
    }
}
