using eNote.Application.Common.Interfaces;
using eNote.Tests.TestUtils;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Testcontainers.MsSql;

namespace eNote.Tests.Integration;

[CollectionDefinition(Name)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
    public const string Name = "SqlServer";
}

public sealed class SqlServerFixture : IAsyncLifetime
{
    private const string SqlServerImage = "mcr.microsoft.com/mssql/server:2022-CU13-ubuntu-22.04";

    private MsSqlContainer _container = null!;

    public async Task InitializeAsync()
    {
        _container = new MsSqlBuilder(SqlServerImage).Build();
        await _container.StartAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    public async Task<SqlServerDatabase> CreateDatabaseAsync()
    {
        var database = new SqlServerDatabase(BuildConnectionString($"enote_test_{Guid.NewGuid():N}"));

        await using var context = database.CreateMigrationContext();
        await context.Database.MigrateAsync();

        return database;
    }

    public SqlServerDatabase CreateUnmigratedDatabase() =>
        new(BuildConnectionString($"enote_test_{Guid.NewGuid():N}"));

    private string BuildConnectionString(string databaseName) =>
        new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = databaseName
        }.ConnectionString;
}

public sealed class SqlServerDatabase(string connectionString)
{
    private static readonly DateTime Now = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);

    public ENoteContext CreateContext() =>
        CreateContext(Now, new StubCurrentActor(isAuthenticated: false));

    public ENoteContext CreateContext(DateTime now, ICurrentUserContext currentUser, params IInterceptor[] interceptors)
    {
        var builder = new DbContextOptionsBuilder<ENoteContext>().UseSqlServer(connectionString);
        builder.AddInterceptors(interceptors);

        var context = new ENoteContext(builder.Options, new FixedClock(now), currentUser);
        TestManufacturers.Seed(context);

        return context;
    }

    public ENoteContext CreateMigrationContext() =>
        new(new DbContextOptionsBuilder<ENoteContext>().UseSqlServer(connectionString).Options, new FixedClock(Now), new StubCurrentActor(isAuthenticated: false));
}
