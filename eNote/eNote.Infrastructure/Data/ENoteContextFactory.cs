using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace eNote.Infrastructure.Data
{
    public sealed class ENoteContextFactory : IDesignTimeDbContextFactory<ENoteContext>
    {
        public ENoteContext CreateDbContext(string[] args)
        {
            LoadDotEnv();

            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("ConnectionStrings__DefaultConnection is missing. Set it in .env or environment variables.");

            var optionsBuilder = new DbContextOptionsBuilder<ENoteContext>();

            optionsBuilder.UseSqlServer(connectionString, sql => sql.MigrationsAssembly("eNote.Infrastructure"));

            return new ENoteContext(optionsBuilder.Options);
        }

        private static void LoadDotEnv()
        {
            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

            while (directory is not null)
            {
                var envFile = Path.Combine(directory.FullName, ".env");

                if (File.Exists(envFile))
                {
                    Env.Load(envFile);
                    return;
                }

                directory = directory.Parent;
            }
        }
    }
}
