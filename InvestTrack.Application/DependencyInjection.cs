using InvestTrack.Application.Interfaces;
using InvestTrack.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace InvestTrack.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();
            return services;
        }
    }
}
