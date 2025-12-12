using eNote.Application.Interfaces;
using eNote.Application.Services;

namespace eNote.API.Extensions
{
    public static class ServiceCollectionExtensions
    {       
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ITokenService, TokenService>();

            return services;
        }                        
    }
}
