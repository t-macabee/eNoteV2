using eNote.Application.Interfaces;
using eNote.Application.Interfaces.Identity;
using eNote.Application.Interfaces.Instruments;
using eNote.Application.Interfaces.Instruments.InstrumentRentals;
using eNote.Application.Interfaces.Ports;
using eNote.Application.Services;
using eNote.Application.Services.Instruments;
using eNote.Application.Services.Instruments.Rentals;
using eNote.Infrastructure.Data;
using eNote.Infrastructure.Identity;

namespace eNote.API.Extensions
{
    public static class ApplicationServicesExtensions
    {       
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IInstrumentService, InstrumentService>();

            services.AddScoped<IRentalService, RentalService>();
            services.AddScoped<IRentalQueryService, RentalQueryService>();
            services.AddScoped<IRentalCommandService, RentalCommandService>();

            services.AddScoped<IUserIdentityService, UserIdentityService>();
            services.AddScoped<IAppDbContext>(x => x.GetRequiredService<ENoteContext>());

            return services;
        }       
    }
}
