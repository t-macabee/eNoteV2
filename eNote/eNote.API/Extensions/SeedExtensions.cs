using eNote.Application.Common.Persistence;
using eNote.Infrastructure.Data.Seed;
using eNote.Application.Common.Time;
using eNote.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace eNote.API.Extensions;

public static class SeedExtensions
{
    public static async Task<WebApplication> MigrateAsync(this WebApplication app)
    {
        using IServiceScope scope = app.Services.CreateScope();

        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        await runner.MigrateAsync();

        return app;
    }

    public static async Task<WebApplication> SeedDevelopmentData(this WebApplication app)
    {
        using IServiceScope scope = app.Services.CreateScope();

        var services = scope.ServiceProvider;
        await IdentitySeed.SeedAsync(services);

        var context = services.GetRequiredService<ENoteContext>();
        var clock = services.GetRequiredService<IClock>();
        await DevelopmentDataSeed.SeedAsync(context, clock);

        return app;
    }
}
