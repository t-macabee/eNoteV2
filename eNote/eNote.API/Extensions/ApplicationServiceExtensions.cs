using eNote.API.Services;
using eNote.Application;
using eNote.Application.Common.Interfaces;
using eNote.Application.Features.Identity.Auth.Services;
using eNote.Application.Features.Reports.Services;
using eNote.Infrastructure.Identity;
using eNote.Infrastructure.Reports;

namespace eNote.API.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.AddHttpContextAccessor();
        services.AddSignalR();

        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IAuthService, AuthService>();

        services.AddApplication();

        return services;
    }
}
