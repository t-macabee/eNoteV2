using eNote.Application.Common.Time;
using eNote.Application.Interfaces;
using eNote.Application.Interfaces.Identity;
using eNote.Application.Interfaces.InstrumentRentals;
using eNote.Application.Interfaces.Instruments;
using eNote.Application.Interfaces.Ports;
using eNote.Application.Services;
using eNote.Application.Services.InstrumentRentals;
using eNote.Application.Services.Instruments;
using eNote.Infrastructure.Data;
using eNote.Infrastructure.Identity;

namespace eNote.API.Extensions
{
    public static class ApplicationServicesExtensions
    {       
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddSingleton<IClock, SystemClock>();

            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ITokenService, TokenService>();

            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IUserIdentityService, UserIdentityService>();

            services.AddScoped<IInstrumentService, InstrumentService>();
            services.AddScoped<IRentalService, RentalService>();
            services.AddScoped<IRentalQueryService, RentalQueryService>();
            services.AddScoped<IRentalCommandService, RentalCommandService>();

            services.AddScoped<IAppDbContext>(x => x.GetRequiredService<ENoteContext>());

            return services;
        }       
    }
}
