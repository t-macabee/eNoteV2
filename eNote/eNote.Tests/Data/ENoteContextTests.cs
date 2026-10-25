using eNote.Application.Common.Interfaces;
using eNote.Application.Constants;
using eNote.Domain.Entities.Assignments;
using eNote.Domain.Entities.Shared;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace eNote.Tests.Data;

public sealed class ENoteContextTests
{
    private static readonly DateTime Baseline = new(2026, 7, 3, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Later = new(2026, 7, 3, 14, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(typeof(Assignment))]
    [InlineData(typeof(Course))]
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

    [Theory]
    [InlineData(typeof(Instrument))]
    [InlineData(typeof(InstrumentRental))]
    public void Model_RentalCatalogEntities_HaveTenantOnlyQueryFilter(Type entityType)
    {
        using var context = CreateContext(Baseline);

        var filter = context.Model.FindEntityType(entityType)?
            .GetDeclaredQueryFilters()
            .Select(queryFilter => queryFilter.Expression)
            .SingleOrDefault();

        Assert.NotNull(filter);
        Assert.Contains("MusicStoreId", filter.ToString());
        Assert.DoesNotContain("IsActive", filter.ToString());
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
    public void Model_MusicStoreEmployee_HasNoSoftDeleteQueryFilter()
    {
        using var context = CreateContext(Baseline);

        var filter = context.Model.FindEntityType(typeof(MusicStoreEmployee))?
            .GetDeclaredQueryFilters()
            .Select(queryFilter => queryFilter.Expression)
            .SingleOrDefault();

        Assert.NotNull(filter);
        Assert.DoesNotContain("IsActive", filter.ToString());
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
    public async Task SaveChangesAsync_SetsCreatedById_WhenEntityAdded()
    {
        await using var context = CreateContextWithActor(Baseline, new StubCurrentActor(userId: 42));
        var student = new Student(appUserId: 100, enrollmentDate: Baseline);

        context.Set<Student>().Add(student);
        await context.SaveChangesAsync();

        Assert.Equal(42, student.CreatedById);
        Assert.Null(student.UpdatedById);
    }

    [Fact]
    public async Task SaveChangesAsync_SetsUpdatedById_WhenEntityModified()
    {
        var dbName = Guid.NewGuid().ToString();

        await using (var arrange = CreateContextWithActor(Baseline, new StubCurrentActor(userId: 42), dbName))
        {
            arrange.Set<Student>().Add(new Student(100, Baseline));
            await arrange.SaveChangesAsync();
        }

        await using var act = CreateContextWithActor(Later, new StubCurrentActor(userId: 43), dbName);
        var student = await act.Set<Student>().FirstAsync();

        student.UpdateMembership(Later.AddDays(30));
        await act.SaveChangesAsync();

        Assert.Equal(42, student.CreatedById);
        Assert.Equal(43, student.UpdatedById);
    }

    [Fact]
    public async Task SaveChangesAsync_LeavesAuditUserIdsNull_WhenActorIsUnauthenticated()
    {
        var dbName = Guid.NewGuid().ToString();

        await using (var arrange = CreateContextWithActor(Baseline, new StubCurrentActor(isAuthenticated: false), dbName))
        {
            arrange.Set<Student>().Add(new Student(100, Baseline));
            await arrange.SaveChangesAsync();
        }

        await using var act = CreateContextWithActor(Later, new StubCurrentActor(isAuthenticated: false), dbName);
        var student = await act.Set<Student>().FirstAsync();

        student.UpdateMembership(Later.AddDays(30));
        await act.SaveChangesAsync();

        Assert.Null(student.CreatedById);
        Assert.Null(student.UpdatedById);
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

        Assert.Equal(Baseline, student.CreatedAt);
        Assert.Equal(Later, student.UpdatedAt);
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

        await act.SaveChangesAsync();

        var count = await act.Set<Student>().CountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    public void Model_PaymentRefund_RequiresExactlyOnePaymentForeignKeys()
    {
        using var context = CreateContext(Baseline);

        var entityType = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(PaymentRefund));
        Assert.NotNull(entityType);

        var check = Assert.Single(entityType.GetCheckConstraints());
        Assert.Equal(DbConstraintNames.PaymentRefundExactlyOnePaymentCheck, check.Name);
        Assert.Contains("RentalPaymentId", check.Sql);
        Assert.Contains("CoursePaymentId", check.Sql);

        var foreignKeyProperties = entityType.GetForeignKeys()
            .SelectMany(fk => fk.Properties)
            .Select(p => p.Name)
            .OrderBy(name => name)
            .ToList();
        Assert.Equal(["CoursePaymentId", "RentalPaymentId"], foreignKeyProperties);

        Assert.True(entityType.FindProperty(nameof(PaymentRefund.RentalPaymentId))!.IsNullable);
        Assert.True(entityType.FindProperty(nameof(PaymentRefund.CoursePaymentId))!.IsNullable);
    }

    [Fact]
    public void Model_PaymentRefund_HasUniqueStripeRefundIdIndex()
    {
        using var context = CreateContext(Baseline);

        var index = context.Model.FindEntityType(typeof(PaymentRefund))!
            .GetIndexes()
            .Single(i => i.Properties.Select(p => p.Name).SequenceEqual([nameof(PaymentRefund.StripeRefundId)]));

        Assert.True(index.IsUnique);
        Assert.Equal(DbConstraintNames.PaymentRefundStripeRefundIdUniqueIndex, index.GetDatabaseName());
    }

    [Fact]
    public void Model_MembershipPayment_HasUniqueReceiptNumberIndex()
    {
        using var context = CreateContext(Baseline);

        var entityType = context.Model.FindEntityType(typeof(MembershipPayment))!;
        var index = entityType.GetIndexes()
            .Single(i => i.Properties.Select(p => p.Name).SequenceEqual([nameof(MembershipPayment.ReceiptNumber)]));

        Assert.True(index.IsUnique);
        Assert.Equal(DbConstraintNames.MembershipPaymentReceiptNumberUniqueIndex, index.GetDatabaseName());
        Assert.False(entityType.FindProperty(nameof(MembershipPayment.ReceiptNumber))!.IsNullable);
        Assert.Equal(50, entityType.FindProperty(nameof(MembershipPayment.ReceiptNumber))!.GetMaxLength());
        Assert.Equal(500, entityType.FindProperty(nameof(MembershipPayment.Note))!.GetMaxLength());
        Assert.False(entityType.FindProperty(nameof(MembershipPayment.Amount))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(MembershipPayment.PeriodStart))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(MembershipPayment.PeriodEnd))!.IsNullable);

        var foreignKey = entityType.GetForeignKeys()
            .Single(fk => fk.Properties.Select(p => p.Name).SequenceEqual([nameof(MembershipPayment.StudentId)]));
        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
    }

    [Fact]
    public void Model_Payment_HasOneOpenAttemptPerObligationUniqueIndex()
    {
        using var context = CreateContext(Baseline);

        AssertOpenAttemptIndex<CoursePayment>(context, nameof(CoursePayment.EnrollmentId), DbConstraintNames.CoursePaymentOpenAttemptUniqueIndex);
        AssertOpenAttemptIndex<RentalPayment>(context, nameof(RentalPayment.InstrumentRentalId), DbConstraintNames.RentalPaymentOpenAttemptUniqueIndex);
    }

    [Fact]
    public void Model_CoursePayment_HasRowVersionConcurrencyToken()
    {
        using var context = CreateContext(Baseline);

        var version = context.Model.FindEntityType(typeof(CoursePayment))!
            .FindProperty(nameof(StripePayment.Version));

        Assert.NotNull(version);
        Assert.True(version.IsConcurrencyToken);
        Assert.Equal(ValueGenerated.OnAddOrUpdate, version.ValueGenerated);
    }

    [Fact]
    public async Task SaveChangesAsync_PersistsCourseRefund_WithCoursePaymentForeignKey()
    {
        await using var context = CreateContext(Baseline);
        var payment = new CoursePayment(1, "pi_test_1", 5000, "bam", PaymentStatus.Succeeded);
        payment.ApplyRefund(2000, "re_test_1", Baseline);

        context.Set<CoursePayment>().Add(payment);
        await context.SaveChangesAsync();

        var stored = await context.Set<PaymentRefund>().SingleAsync();
        Assert.Equal(payment.Id, stored.CoursePaymentId);
        Assert.Null(stored.RentalPaymentId);
        Assert.Equal(2000, stored.AmountCents);
    }

    private static void AssertOpenAttemptIndex<TEntity>(ENoteContext context, string obligationProperty, string indexName)
        where TEntity : class
    {
        var index = context.Model.FindEntityType(typeof(TEntity))!
            .GetIndexes()
            .Single(i => i.GetDatabaseName() == indexName);

        Assert.True(index.IsUnique);
        Assert.Equal([obligationProperty], index.Properties.Select(p => p.Name));
        Assert.Equal("\"Status\" IN (1, 3)", index.GetFilter());
    }

    private static ENoteContext CreateContext(DateTime now, string? databaseName = null) =>
        TestDbContextFactory.CreateContext(now, databaseName);

    private static ENoteContext CreateContextWithActor(DateTime now, ICurrentUserContext currentUser, string? databaseName = null) =>
        TestDbContextFactory.CreateContext(now, databaseName, currentUser);
}
