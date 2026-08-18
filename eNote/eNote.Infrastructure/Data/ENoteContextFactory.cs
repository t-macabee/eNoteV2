using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Time;
using eNote.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace eNote.Infrastructure.Data;

public sealed class ENoteContextFactory : IDesignTimeDbContextFactory<ENoteContext>
{
    public ENoteContext CreateDbContext(string[] args)
    {
        DotEnvConfiguration.Load();

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

    private sealed class DesignTimeActor : ICurrentActor
    {
        public int UserId => 1;
        public bool IsAuthenticated => true;
        public Task<Student> GetCurrentStudentAsync() => Task.FromException<Student>(new NotSupportedException());
        public Task<int> GetCurrentStudentIdAsync() => Task.FromException<int>(new NotSupportedException());
        public Task<Instructor> GetCurrentInstructorAsync() => Task.FromException<Instructor>(new NotSupportedException());
        public Task<MusicStoreEmployee> GetCurrentEmployeeAsync() => Task.FromException<MusicStoreEmployee>(new NotSupportedException());
        public Task<int> GetCurrentStoreIdAsync(CancellationToken cancellationToken = default) => Task.FromResult(1); // design-time only: single-store seed context
        public int GetCurrentStoreId() => 1;
    }
}
