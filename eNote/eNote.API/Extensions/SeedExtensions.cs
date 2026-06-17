using eNote.Infrastructure.Data;
using eNote.Infrastructure.Data.Seed;

namespace eNote.API.Extensions
{
    public static class SeedExtensions
    {
        public static async Task<WebApplication> SeedDevelopmentData(this WebApplication app)
        {
            using IServiceScope scope = app.Services.CreateScope();

            IServiceProvider services = scope.ServiceProvider;
            await IdentitySeed.SeedAsync(services);

            ENoteContext context = services.GetRequiredService<ENoteContext>();
            await DevelopmentDataSeed.SeedAsync(context);

            return app;
        }
    }
}
