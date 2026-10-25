using eNote.Tests.TestUtils;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
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
        var database = new SqlServerDatabase(BuildOptions($"enote_test_{Guid.NewGuid():N}"));

        await using var context = database.CreateContext();
        await context.Database.MigrateAsync();

        return database;
    }

    private DbContextOptions<ENoteContext> BuildOptions(string databaseName)
    {
        var connectionString = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = databaseName
        }.ConnectionString;

        return new DbContextOptionsBuilder<ENoteContext>().UseSqlServer(connectionString).Options;
    }
}

public sealed class SqlServerDatabase(DbContextOptions<ENoteContext> options)
{
    private static readonly DateTime Now = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);

    public ENoteContext CreateContext() =>
        new(options, new FixedClock(Now), new StubCurrentActor(isAuthenticated: false));
}
