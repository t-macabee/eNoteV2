using eNote.Application.Features.Identity.Instructors;
using eNote.Application.Features.Identity.Users;
using eNote.Tests.TestUtils;

namespace eNote.Tests.Identity;

public sealed class AdminInstructorServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetPagedAsync_FiltersByNameAndUsername()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        context.Set<Instructor>().AddRange(
            new Instructor(1),
            new Instructor(2),
            new Instructor(3));
        await context.SaveChangesAsync();
        var identity = new StubUserIdentityService(new Dictionary<int, UserIdentityDto>
        {
            [1] = StubUserIdentityService.User(1, "jdoe", "Jane", "Doe"),
            [2] = StubUserIdentityService.User(2, "asmith", "Alice", "Smith"),
            [3] = StubUserIdentityService.User(3, "brown", "Bob", "Brown")
        });
        var service = new AdminInstructorService(context, identity);

        var result = await service.GetPagedAsync(new InstructorSearchObject { Name = "Smith" });

        var dto = Assert.Single(result.Items);
        Assert.Equal("Alice", dto.FirstName);
        Assert.Equal("Smith", dto.LastName);
    }

    [Fact]
    public async Task GetPagedAsync_RespectsPaging()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        context.Set<Instructor>().AddRange(
            new Instructor(1),
            new Instructor(2),
            new Instructor(3));
        await context.SaveChangesAsync();
        var identity = new StubUserIdentityService();
        var service = new AdminInstructorService(context, identity);

        var result = await service.GetPagedAsync(new InstructorSearchObject { Page = 2, PageSize = 1 });

        var dto = Assert.Single(result.Items);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, dto.AppUserId);
    }

    [Fact]
    public async Task GetByIdAsync_Throws_WhenInstructorMissing()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var service = new AdminInstructorService(context, new StubUserIdentityService());

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(999));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsMappedInstructor()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        context.Set<Instructor>().Add(new Instructor(42));
        await context.SaveChangesAsync();
        var identity = new StubUserIdentityService(new Dictionary<int, UserIdentityDto>
        {
            [42] = StubUserIdentityService.User(42, "jdoe", "Jane", "Doe")
        });
        var service = new AdminInstructorService(context, identity);

        var dto = await service.GetByIdAsync(1);

        Assert.Equal(42, dto.AppUserId);
        Assert.Equal("Jane", dto.FirstName);
        Assert.Equal("jdoe", dto.Username);
    }
}
