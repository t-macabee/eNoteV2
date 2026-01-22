using eNote.Infrastructure.Data;
using eNote.Infrastructure.Data.Seed;

namespace eNote.API.Extensions
{
    public static class SeedExtensions
    {
        public static async Task<WebApplication> SeedDevelopmentData(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();

            var services = scope.ServiceProvider;
            await IdentitySeed.SeedAsync(services);

            var context = services.GetRequiredService<ENoteContext>();
            await DevelopmentDataSeed.SeedAsync(context);

            return app;
        }
    }
}
