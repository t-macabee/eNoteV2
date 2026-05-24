using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Auth.Services.Interfaces;
using eNote.Application.Features.InstrumentRentals.Services.Interfaces;
using eNote.Application.Features.InstrumentRentals.Services;
using eNote.Application.Features.Announcements.Services;
using eNote.Application.Features.Announcements.Services.Interfaces;
using eNote.Application.Features.Instruments.Services;
using eNote.Application.Features.Instruments.Services.Interfaces;
using eNote.Application.Features.Users.Services;
using eNote.Application.Features.Users.Services.Interfaces;
using eNote.Infrastructure.Data;
using eNote.Infrastructure.Identity;
using eNote.Application.Features.MusicStores.Services;
using eNote.Application.Features.MusicStores.Services.Interfaces;

namespace eNote.API.Extensions
{
    public static class ApplicationServiceExtensions
    {       
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddSingleton<IClock, SystemClock>();

            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ITokenService, TokenService>();

            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IUserIdentityService, UserIdentityService>();

            services.AddScoped<IInstrumentService, InstrumentService>();
            services.AddScoped<IRentalQueryService, RentalQueryService>();
            services.AddScoped<IRentalCommandService, RentalCommandService>();

            services.AddScoped<IAnnouncementService, AnnouncementService>();

            services.AddScoped<IAppDbContext>(x => x.GetRequiredService<ENoteContext>());
            services.AddScoped<IMusicStoreContextService, MusicStoreContextService>();

            return services;
        }       
    }
}
