using System.Reflection;
using eNote.Application.Common.Persistence;
using eNote.Application.Constants;
using eNote.Domain.Entities.Rentals;
using eNote.Infrastructure.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace eNote.Tests.Integration;

[Collection(SqlServerCollection.Name)]
public sealed class DatabaseConstraintTests(SqlServerFixture fixture)
{
    private static readonly DateTime Now = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Migrations_LeaveNoPendingMigration()
    {
        var database = await fixture.CreateDatabaseAsync();
        await using var context = database.CreateContext();

        var pending = await context.Database.GetPendingMigrationsAsync();

        Assert.Empty(pending);
    }

    [Fact]
    public async Task ConstraintNames_ExistInDatabase()
    {
        var database = await fixture.CreateDatabaseAsync();
        await using var context = database.CreateContext();

        var databaseNames = (await context.Database
            .SqlQueryRaw<string>(
                """
                SELECT name AS Value FROM sys.indexes WHERE name IS NOT NULL
                UNION
                SELECT name AS Value FROM sys.check_constraints
                """)
            .ToListAsync())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var constraintNames = typeof(DbConstraintNames)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral)
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToList();

        Assert.NotEmpty(constraintNames);
        Assert.All(constraintNames, name => Assert.Contains(name, databaseNames));
    }

    [Fact]
    public async Task DuplicateStripeWebhookEventId_IsUniqueViolation()
    {
        var database = await fixture.CreateDatabaseAsync();
        await using var context = database.CreateContext();

        context.Set<StripeWebhookEvent>().Add(new StripeWebhookEvent("evt_duplicate", "payment_intent.succeeded", "{}", Now));
        await context.SaveChangesAsync();

        context.Set<StripeWebhookEvent>().Add(new StripeWebhookEvent("evt_duplicate", "payment_intent.succeeded", "{}", Now));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        Assert.True(DbErrors.IsUniqueViolation(exception, DbConstraintNames.StripeWebhookEventStripeEventIdUniqueIndex));
    }

    [Fact]
    public async Task SecondOpenCoursePaymentAttempt_IsUniqueViolation()
    {
        var database = await fixture.CreateDatabaseAsync();
        await using var context = database.CreateContext();

        var (_, _, _, enrollment) = await SeedCourseEnrollmentAsync(context);

        context.Set<CoursePayment>().Add(
            new CoursePayment(enrollment.Id, "pi_open_attempt_1", 10000, "bam", PaymentStatus.RequiresAction));
        await context.SaveChangesAsync();

        context.Set<CoursePayment>().Add(
            new CoursePayment(enrollment.Id, "pi_open_attempt_2", 10000, "bam", PaymentStatus.RequiresAction));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        Assert.True(DbErrors.IsUniqueViolation(exception, DbConstraintNames.CoursePaymentOpenAttemptUniqueIndex));
    }

    [Fact]
    public async Task DuplicateMembershipReceiptNumber_IsUniqueViolation()
    {
        var database = await fixture.CreateDatabaseAsync();
        await using var context = database.CreateContext();

        var (student, _, _, _) = await SeedCourseEnrollmentAsync(context);

        context.Set<MembershipPayment>().Add(
            new MembershipPayment(student.Id, 50m, Now, Now, Now.AddMonths(1), "REC-1", null));
        await context.SaveChangesAsync();

        context.Set<MembershipPayment>().Add(
            new MembershipPayment(student.Id, 50m, Now.AddDays(1), Now, Now.AddMonths(1), "REC-1", null));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        Assert.True(DbErrors.IsUniqueViolation(exception, DbConstraintNames.MembershipPaymentReceiptNumberUniqueIndex));
    }

    [Fact]
    public async Task ConcurrentEnrollmentUpdate_ThrowsConcurrencyException()
    {
        var database = await fixture.CreateDatabaseAsync();

        var enrollmentId = 0;

        await using (var seedContext = database.CreateContext())
        {
            var (_, _, _, enrollment) = await SeedCourseEnrollmentAsync(seedContext);
            enrollmentId = enrollment.Id;
        }

        await using var firstContext = database.CreateContext();
        await using var secondContext = database.CreateContext();

        var first = await firstContext.Set<Enrollment>().SingleAsync(e => e.Id == enrollmentId);
        var second = await secondContext.Set<Enrollment>().SingleAsync(e => e.Id == enrollmentId);

        Assert.True(first.Transition(EnrollmentTrigger.Cancel, 20, Now).IsSuccess);
        Assert.True(second.Transition(EnrollmentTrigger.Complete, 20, Now).IsSuccess);

        await firstContext.SaveChangesAsync();

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => secondContext.SaveChangesAsync());
    }

    [Fact]
    public async Task CourseWithUnknownCreatedById_IsForeignKeyViolation()
    {
        var database = await fixture.CreateDatabaseAsync();
        await using var context = database.CreateContext();

        var (_, instructor, _, _) = await SeedCourseEnrollmentAsync(context);

        var course = new Course("Guitar", null, 100m, Now, Now.AddMonths(6), instructor.Id) { CreatedById = 999999 };
        context.Set<Course>().Add(course);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        var sqlException = Assert.IsType<SqlException>(exception.InnerException);
        Assert.Equal(547, sqlException.Number);
    }

    private static async Task<(Student Student, Instructor Instructor, Course Course, Enrollment Enrollment)> SeedCourseEnrollmentAsync(
        ENoteContext context)
    {
        var studentUser = new AppUser
        {
            UserName = "student",
            NormalizedUserName = "STUDENT",
            Email = "student@example.com",
            NormalizedEmail = "STUDENT@EXAMPLE.COM",
            IsActive = true
        };
        var instructorUser = new AppUser
        {
            UserName = "instructor",
            NormalizedUserName = "INSTRUCTOR",
            Email = "instructor@example.com",
            NormalizedEmail = "INSTRUCTOR@EXAMPLE.COM",
            IsActive = true
        };
        context.Set<AppUser>().AddRange(studentUser, instructorUser);
        await context.SaveChangesAsync();

        var student = new Student(studentUser.Id, Now.AddMonths(-1));
        var instructor = new Instructor(instructorUser.Id);
        context.Set<Student>().Add(student);
        context.Set<Instructor>().Add(instructor);
        await context.SaveChangesAsync();

        var course = new Course("Guitar", null, 100m, Now, Now.AddMonths(6), instructor.Id);
        course.SetPublishedStatus(true);
        context.Set<Course>().Add(course);
        await context.SaveChangesAsync();

        var enrollment = new Enrollment(student.Id, course.Id, EnrollmentStatus.Active);
        context.Set<Enrollment>().Add(enrollment);
        await context.SaveChangesAsync();

        return (student, instructor, course, enrollment);
    }
}
