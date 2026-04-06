using ATOZA.Application.Abstractions.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ATOZA.Application
{
    /// <summary>
    /// Đăng ký tất cả services của tầng Application vào DI container
    /// Gọi trong Program.cs: builder.Services.AddApplication();
    /// </summary>
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // Services sẽ được implement ở Infrastructure và đăng ký ở đó
            // File này dành cho các service thuần Application (không cần DB/IO)
            return services;
        }
    }
}
