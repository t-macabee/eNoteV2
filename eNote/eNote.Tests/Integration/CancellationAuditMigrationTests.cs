using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace eNote.Tests.Integration;

[Collection(SqlServerCollection.Name)]
public sealed class CancellationAuditMigrationTests(SqlServerFixture fixture)
{
    private const string PreviousMigration = "20261107212921_AddManufacturerTable";
    private const int PendingStatus = 1;
    private const int ApprovedStatus = 2;
    private const int ActiveStatus = 3;
    private const int RejectedStatus = 5;

    [Fact]
    public async Task Migration_BackfillsDecisionNote_ForApprovedAndRejectedRentalsOnly()
    {
        var database = fixture.CreateUnmigratedDatabase();
        await using var context = database.CreateMigrationContext();
        var migrator = context.GetService<IMigrator>();

        await migrator.MigrateAsync(PreviousMigration);
        await context.Database.ExecuteSqlRawAsync("ALTER TABLE InstrumentRental NOCHECK CONSTRAINT ALL");
        await InsertRentalAsync(context, 1, ApprovedStatus, "Odobreno A");
        await InsertRentalAsync(context, 2, RejectedStatus, "Odbijeno B");
        await InsertRentalAsync(context, 3, ActiveStatus, "Povrat C");
        await InsertRentalAsync(context, 4, PendingStatus, "Otkazano D");

        await migrator.MigrateAsync();

        var notes = await context.Database
            .SqlQueryRaw<string>("SELECT ISNULL(DecisionNote, '-') AS Value FROM InstrumentRental ORDER BY Id")
            .ToListAsync();
        Assert.Equal(["Odobreno A", "Odbijeno B", "-", "-"], notes);
    }

    private static Task<int> InsertRentalAsync(DbContext context, int id, int status, string note) =>
        context.Database.ExecuteSqlRawAsync(
            """
            SET IDENTITY_INSERT InstrumentRental ON;
            INSERT INTO InstrumentRental (Id, InstrumentId, StudentProfileId, MusicStoreId, RentalStatus, Fee, OverduePenaltyMultiplier, IsPaid, RequestedAt, CreatedAt, Note, RejectedAt)
            VALUES ({0}, {0}, {0}, 1, {1}, 10, 1, 0, SYSUTCDATETIME(), SYSUTCDATETIME(), {2}, CASE WHEN {1} = 5 THEN SYSUTCDATETIME() END);
            SET IDENTITY_INSERT InstrumentRental OFF;
            """,
            id, status, note);
}
