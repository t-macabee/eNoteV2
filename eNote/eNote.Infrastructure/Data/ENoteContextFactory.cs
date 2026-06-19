using DotNetEnv;
using eNote.Application.Common.Time;
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

            IConfigurationRoot configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            string connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("ConnectionStrings__DefaultConnection is missing. Set it in .env or environment variables.");

            DbContextOptionsBuilder<ENoteContext> optionsBuilder = new DbContextOptionsBuilder<ENoteContext>();
            optionsBuilder.UseSqlServer(connectionString, sql => sql.MigrationsAssembly("eNote.Infrastructure"));

            return new ENoteContext(optionsBuilder.Options, new SystemClock());
        }

        private static void LoadDotEnv()
        {
            DirectoryInfo directory = new DirectoryInfo(Directory.GetCurrentDirectory());

            while (directory is not null)
            {
                string envFile = Path.Combine(directory.FullName, ".env");

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
