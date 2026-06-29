using DotNetEnv;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Time;
using eNote.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace eNote.Infrastructure.Data;

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
        optionsBuilder.UseNpgsql(connectionString, sql => sql.MigrationsAssembly("eNote.Infrastructure"));

        var actor = new DesignTimeActor();
        return new ENoteContext(optionsBuilder.Options, new SystemClock(), actor);
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

    private sealed class DesignTimeActor : ICurrentActor
    {
        public int UserId => 1;
        public bool IsAuthenticated => true;
        public Task<Student> GetCurrentStudentAsync() => throw new NotSupportedException();
        public Task<int> GetCurrentStudentIdAsync() => throw new NotSupportedException();
        public Task<Instructor> GetCurrentInstructorAsync() => throw new NotSupportedException();
        public Task<MusicStoreEmployee> GetCurrentEmployeeAsync() => throw new NotSupportedException();
        public Task<int> GetCurrentStoreIdAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
        public int GetCurrentStoreId() => 1;
    }
}
