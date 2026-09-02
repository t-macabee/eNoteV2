using eNote.Application.Features.Identity.Students;
using eNote.Application.Features.Identity.Users;
using eNote.Tests.TestUtils;

namespace eNote.Tests.Identity;

public sealed class AdminStudentServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetPagedAsync_FiltersByNameAndUsername()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        context.Set<Student>().AddRange(
            new Student(1, Now),
            new Student(2, Now),
            new Student(3, Now));
        await context.SaveChangesAsync();
        var identity = new StubUserIdentityService(new Dictionary<int, UserIdentityDto>
        {
            [1] = StubUserIdentityService.User(1, "jdoe", "Jane", "Doe"),
            [2] = StubUserIdentityService.User(2, "asmith", "Alice", "Smith"),
            [3] = StubUserIdentityService.User(3, "brown", "Bob", "Brown")
        });
        var service = new AdminStudentService(context, identity);

        var result = await service.GetPagedAsync(new StudentSearchObject { Name = "Smith" });

        var dto = Assert.Single(result.Items);
        Assert.Equal("Alice", dto.FirstName);
        Assert.Equal("Smith", dto.LastName);
    }

    [Fact]
    public async Task GetPagedAsync_RespectsPaging()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        context.Set<Student>().AddRange(
            new Student(1, Now),
            new Student(2, Now),
            new Student(3, Now));
        await context.SaveChangesAsync();
        var identity = new StubUserIdentityService();
        var service = new AdminStudentService(context, identity);

        var result = await service.GetPagedAsync(new StudentSearchObject { Page = 2, PageSize = 1 });

        var dto = Assert.Single(result.Items);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, dto.AppUserId);
    }

    [Fact]
    public async Task GetByIdAsync_Throws_WhenStudentMissing()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var service = new AdminStudentService(context, new StubUserIdentityService());

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(999));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsMappedStudent()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var student = new Student(42, Now);
        student.UpdateMembership(Now.AddMonths(1));
        context.Set<Student>().Add(student);
        await context.SaveChangesAsync();
        var identity = new StubUserIdentityService(new Dictionary<int, UserIdentityDto>
        {
            [42] = StubUserIdentityService.User(42, "jdoe", "Jane", "Doe")
        });
        var service = new AdminStudentService(context, identity);

        var dto = await service.GetByIdAsync(1);

        Assert.Equal(42, dto.AppUserId);
        Assert.Equal("Jane", dto.FirstName);
        Assert.Equal("jdoe", dto.Username);
        Assert.Equal(Now, dto.EnrollmentDate);
        Assert.Equal(Now.AddMonths(1), dto.MembershipPaidUntil);
    }
}
