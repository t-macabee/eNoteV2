using eNote.Tests.TestUtils;
using eNote.Domain.Entities.Assignments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace eNote.Tests.Data;

public sealed class ENoteContextTests
{
    private static readonly DateTime Baseline = new(2026, 7, 3, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Later = new(2026, 7, 3, 14, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(typeof(Assignment))]
    [InlineData(typeof(Course))]
    [InlineData(typeof(Instrument))]
    [InlineData(typeof(Lecture))]
    [InlineData(typeof(LectureNote))]
    public void Model_SoftDeletableEntities_HaveActiveQueryFilter(Type entityType)
    {
        using var context = CreateContext(Baseline);

        var filter = context.Model.FindEntityType(entityType)?
            .GetDeclaredQueryFilters()
            .Select(queryFilter => queryFilter.Expression)
            .SingleOrDefault();

        Assert.NotNull(filter);
        Assert.Contains("IsActive", filter.ToString());
    }

    [Fact]
    public void Model_MusicStoreEmployee_HasTenantQueryFilter()
    {
        using var context = CreateContext(Baseline);

        var filter = context.Model.FindEntityType(typeof(MusicStoreEmployee))?
            .GetDeclaredQueryFilters()
            .Select(queryFilter => queryFilter.Expression)
            .SingleOrDefault();

        Assert.NotNull(filter);
        Assert.Contains("MusicStoreId", filter.ToString());
    }

    [Fact]
    public async Task SaveChangesAsync_SetsCreatedAt_WhenEntityAdded()
    {
        await using var context = CreateContext(Baseline);
        var student = new Student(appUserId: 100, enrollmentDate: Baseline);

        context.Set<Student>().Add(student);
        await context.SaveChangesAsync();

        Assert.Equal(Baseline, student.CreatedAt);
        Assert.Null(student.UpdatedAt);
    }

    [Fact]
    public async Task SaveChangesAsync_DoesNotTouchTimestamps_WhenEntityUnchanged()
    {
        var dbName = Guid.NewGuid().ToString();

        await using (var arrange = CreateContext(Baseline, dbName))
        {
            arrange.Set<Student>().Add(new Student(100, Baseline));
            await arrange.SaveChangesAsync();
        }

        await using var act = CreateContext(Later, dbName);
        var student = await act.Set<Student>().FirstAsync();

        Assert.Equal(EntityState.Unchanged, act.Entry(student).State);

        await act.SaveChangesAsync();

        // Stamps untouched — entity was never in Added or Modified state in this context
        Assert.Equal(Baseline, student.CreatedAt);
        Assert.Null(student.UpdatedAt);
    }

    [Fact]
    public async Task SaveChangesAsync_SetsUpdatedAt_WhenEntityModified()
    {
        var dbName = Guid.NewGuid().ToString();

        await using (var arrange = CreateContext(Baseline, dbName))
        {
            arrange.Set<Student>().Add(new Student(100, Baseline));
            await arrange.SaveChangesAsync();
        }

        await using var act = CreateContext(Later, dbName);
        var student = await act.Set<Student>().FirstAsync();

        student.UpdateMembership(Later.AddDays(30));

        Assert.Equal(EntityState.Modified, act.Entry(student).State);

        await act.SaveChangesAsync();

        Assert.Equal(Baseline, student.CreatedAt); // Original CreatedAt preserved
        Assert.Equal(Later, student.UpdatedAt);    // Set to clock value at modification time
    }

    [Fact]
    public async Task SaveChangesAsync_DoesNotInterfere_WhenEntityDeleted()
    {
        var dbName = Guid.NewGuid().ToString();

        await using (var arrange = CreateContext(Baseline, dbName))
        {
            arrange.Set<Student>().Add(new Student(100, Baseline));
            await arrange.SaveChangesAsync();
        }

        await using var act = CreateContext(Later, dbName);
        var student = await act.Set<Student>().FirstAsync();
        act.Set<Student>().Remove(student);

        Assert.Equal(EntityState.Deleted, act.Entry(student).State);

        // Should complete without exception — Deleted state is not in
        // the Added/Modified set inspected by the timestamp-stamping loop.
        await act.SaveChangesAsync();

        var count = await act.Set<Student>().CountAsync();
        Assert.Equal(0, count);
    }

    private static ENoteContext CreateContext(DateTime now, string? databaseName = null)
    {
        databaseName ??= Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ENoteContext>()
            .UseInMemoryDatabase(databaseName)
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ENoteContext(options, new FixedClock(now), new StubCurrentActor(new Student(0, now)));
    }
}
