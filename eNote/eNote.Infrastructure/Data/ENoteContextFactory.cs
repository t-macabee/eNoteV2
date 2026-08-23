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
        optionsBuilder.UseSqlServer(connectionString, sql => sql.MigrationsAssembly("eNote.Infrastructure"));

        var currentUser = new DesignTimeUserContext();
        return new ENoteContext(optionsBuilder.Options, new SystemClock(), currentUser);
    }

    private sealed class DesignTimeUserContext : ICurrentUserContext
    {
        public int UserId => 1;
        public bool IsAuthenticated => true;
    }
}
