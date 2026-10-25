using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Domain.Entities.Academic;
using eNote.Domain.Entities.Identity;
using eNote.Domain.Entities.Rentals;
using eNote.Domain.Enums;
using eNote.Infrastructure.Identity;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;

namespace eNote.Tests.Integration;

[Collection(SqlServerCollection.Name)]
public sealed class UserDeleteTests(SqlServerFixture fixture)
{
    private static readonly DateTime Now = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task DeleteUserAsync_RejectsStudentWithEnrollment()
    {
        var database = await fixture.CreateDatabaseAsync();

        var userId = await SeedAsync(database, async context =>
        {
            var (studentUser, student, _, course) = await SeedCourseStudentAsync(context);

            context.Set<Enrollment>().Add(new Enrollment(student.Id, course.Id, EnrollmentStatus.Active));
            await context.SaveChangesAsync();

            return studentUser.Id;
        });

        await AssertDeleteIsBlockedAsync(database, userId);
    }

    [Fact]
    public async Task DeleteUserAsync_RejectsUserWhoCancelledLecture()
    {
        var database = await fixture.CreateDatabaseAsync();

        var userId = await SeedAsync(database, async context =>
        {
            var cancellingUser = await AddUserAsync(context, "cancelling");
            var (_, _, _, course) = await SeedCourseStudentAsync(context);

            var lecture = new Lecture("First lesson", "Room 1", 60, Now, LectureType.Theoretical, 30, course.Id);
            lecture.Cancel("Sickness", cancellingUser.Id, Now);

            context.Set<Lecture>().Add(lecture);
            await context.SaveChangesAsync();

            return cancellingUser.Id;
        });

        await AssertDeleteIsBlockedAsync(database, userId);
    }

    [Fact]
    public async Task DeleteUserAsync_RejectsStudentWithIndividualLecture()
    {
        var database = await fixture.CreateDatabaseAsync();

        var userId = await SeedAsync(database, async context =>
        {
            var (studentUser, student, _, course) = await SeedCourseStudentAsync(context);

            context.Set<Lecture>().Add(
                new Lecture("Individual lesson", "Room 3", 45, Now, LectureType.Practical, null, course.Id, student.Id));
            await context.SaveChangesAsync();

            return studentUser.Id;
        });

        await AssertDeleteIsBlockedAsync(database, userId);
    }

    [Fact]
    public async Task DeleteUserAsync_RejectsUserWhoCreatedCourse()
    {
        var database = await fixture.CreateDatabaseAsync();

        var userId = await SeedAsync(database, async context =>
        {
            var creator = await AddUserAsync(context, "creator");
            var (_, _, instructor, _) = await SeedCourseStudentAsync(context);

            context.Set<Course>().Add(
                new Course("Piano", null, 100m, Now, Now.AddMonths(6), instructor.Id) { CreatedById = creator.Id });
            await context.SaveChangesAsync();

            return creator.Id;
        });

        await AssertDeleteIsBlockedAsync(database, userId);
    }

    [Fact]
    public async Task DeleteUserAsync_RemovesStudentWithoutReferences()
    {
        var database = await fixture.CreateDatabaseAsync();

        var userId = await SeedAsync(database, async context =>
        {
            var studentUser = await AddUserAsync(context, "student");
            context.Set<Student>().Add(new Student(studentUser.Id, Now));
            await context.SaveChangesAsync();

            return studentUser.Id;
        });

        await using var deleteContext = database.CreateContext();
        await CreateService(deleteContext).DeleteUserAsync(userId);

        await using var verifyContext = database.CreateContext();

        Assert.False(await verifyContext.Set<AppUser>().AnyAsync(u => u.Id == userId));
        Assert.False(await verifyContext.Set<Student>().AnyAsync(s => s.AppUserId == userId));
    }

    [Fact]
    public async Task DeleteUserAsync_RemovesStoreEmployeeWithoutReferences()
    {
        var database = await fixture.CreateDatabaseAsync();

        var userId = await SeedAsync(database, async context =>
        {
            var employeeUser = await AddUserAsync(context, "employee");

            var store = new MusicStore("Music Store", "09:00-17:00");
            context.Set<MusicStore>().Add(store);
            await context.SaveChangesAsync();

            context.Set<MusicStoreEmployee>().Add(new MusicStoreEmployee(employeeUser.Id, store.Id, isManager: false));
            await context.SaveChangesAsync();

            return employeeUser.Id;
        });

        await using var deleteContext = database.CreateContext();
        await CreateService(deleteContext).DeleteUserAsync(userId);

        await using var verifyContext = database.CreateContext();

        Assert.False(await verifyContext.Set<AppUser>().AnyAsync(u => u.Id == userId));
        Assert.False(await verifyContext.Set<MusicStoreEmployee>().AnyAsync(e => e.AppUserId == userId));
    }

    private static async Task<int> SeedAsync(SqlServerDatabase database, Func<ENoteContext, Task<int>> seed)
    {
        await using var context = database.CreateContext();

        return await seed(context);
    }

    private static async Task AssertDeleteIsBlockedAsync(SqlServerDatabase database, int userId)
    {
        await using var deleteContext = database.CreateContext();

        var exception = await Assert.ThrowsAsync<ConflictException>(() => CreateService(deleteContext).DeleteUserAsync(userId));

        Assert.Equal(Messages.UserDeleteBlocked, exception.Message);

        await using var verifyContext = database.CreateContext();

        Assert.True(await verifyContext.Set<AppUser>().AnyAsync(u => u.Id == userId));
    }

    private static UserProvisioningService CreateService(ENoteContext context)
    {
        var identity = IdentityTestHarness.Create(context);

        return new UserProvisioningService(
            context,
            new UserAccountService(identity.UserManager, new StubFileStorageService()),
            new FixedClock(Now),
            new StubFileStorageService());
    }

    private static async Task<AppUser> AddUserAsync(ENoteContext context, string userName)
    {
        var user = new AppUser
        {
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            Email = $"{userName}@example.com",
            NormalizedEmail = $"{userName}@example.com".ToUpperInvariant(),
            IsActive = true
        };

        context.Set<AppUser>().Add(user);
        await context.SaveChangesAsync();

        return user;
    }

    private static async Task<(AppUser StudentUser, Student Student, Instructor Instructor, Course Course)> SeedCourseStudentAsync(
        ENoteContext context)
    {
        var studentUser = await AddUserAsync(context, "student");
        var instructorUser = await AddUserAsync(context, "instructor");

        var student = new Student(studentUser.Id, Now.AddMonths(-1));
        var instructor = new Instructor(instructorUser.Id);

        context.Set<Student>().Add(student);
        context.Set<Instructor>().Add(instructor);
        await context.SaveChangesAsync();

        var course = new Course("Guitar", null, 100m, Now, Now.AddMonths(6), instructor.Id);
        context.Set<Course>().Add(course);
        await context.SaveChangesAsync();

        return (studentUser, student, instructor, course);
    }
}
