using eNote.Application.Features.Identity.Users;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;

namespace eNote.Tests.Identity;

public sealed class StudentDisplayNameServiceTests
{
    [Fact]
    public async Task GetStudentDisplayNameAsync_UsesFullName()
    {
        var identity = new StubUserIdentityService(new Dictionary<int, UserIdentityDto>
        {
            [5] = StubUserIdentityService.User(5, "jdoe", "Jane", "Doe")
        });
        var service = new StudentDisplayNameService(identity);
        var student = new Student(5, DateTime.UtcNow);

        Assert.Equal("Jane Doe", await service.GetStudentDisplayNameAsync(student));
    }

    [Fact]
    public async Task GetStudentDisplayNameAsync_FallsBackToUsername_WhenNoName()
    {
        var identity = new StubUserIdentityService(new Dictionary<int, UserIdentityDto>
        {
            [5] = StubUserIdentityService.User(5, "jdoe")
        });
        var service = new StudentDisplayNameService(identity);

        Assert.Equal("jdoe", await service.GetStudentDisplayNameAsync(new Student(5, DateTime.UtcNow)));
    }

    [Fact]
    public async Task GetStudentDisplayNameAsync_FallsBackToStudentId_WhenUserMissing()
    {
        await using var context = TestDbContextFactory.CreateContext(new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc));
        context.Set<Student>().Add(new Student(5, DateTime.UtcNow));
        await context.SaveChangesAsync();
        var student = await context.Set<Student>().SingleAsync();
        var service = new StudentDisplayNameService(new StubUserIdentityService());

        Assert.Equal($"Student {student.Id}", await service.GetStudentDisplayNameAsync(student));
    }

    [Fact]
    public async Task GetStudentDisplayNamesAsync_ReturnsPerStudentNames()
    {
        await using var context = TestDbContextFactory.CreateContext(new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc));
        context.Set<Student>().AddRange(
            new Student(5, DateTime.UtcNow),
            new Student(6, DateTime.UtcNow),
            new Student(7, DateTime.UtcNow));
        await context.SaveChangesAsync();
        var students = await context.Set<Student>().OrderBy(s => s.AppUserId).ToListAsync();
        var identity = new StubUserIdentityService(new Dictionary<int, UserIdentityDto>
        {
            [5] = StubUserIdentityService.User(5, "jdoe", "Jane", "Doe"),
            [6] = StubUserIdentityService.User(6, "asmith")
        });
        var service = new StudentDisplayNameService(identity);

        var names = await service.GetStudentDisplayNamesAsync(students);

        Assert.Equal("Jane Doe", names[students[0].Id]);
        Assert.Equal("asmith", names[students[1].Id]);
        Assert.Equal($"Student {students[2].Id}", names[students[2].Id]);
    }
}
