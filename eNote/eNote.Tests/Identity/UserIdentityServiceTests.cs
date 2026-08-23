using eNote.Infrastructure.Identity;
using eNote.Tests.TestUtils;

namespace eNote.Tests.Identity;

public sealed class UserIdentityServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetUserAsync_MapsProfileFields()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var userManager = IdentityTestHarness.Create(context).UserManager;
        var user = new AppUser
        {
            UserName = "jdoe",
            Email = "jdoe@example.com",
            FirstName = "Jane",
            LastName = "Doe",
            IsActive = true
        };
        await userManager.CreateAsync(user, "Password1!");
        var service = new UserIdentityService(userManager);

        var dto = await service.GetUserAsync(user.Id);

        Assert.NotNull(dto);
        Assert.Equal("jdoe", dto.Username);
        Assert.Equal("Jane", dto.FirstName);
        Assert.Equal("Doe", dto.LastName);
        Assert.True(dto.IsActive);
    }

    [Fact]
    public async Task GetUserAsync_ReturnsNull_WhenMissing()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var service = new UserIdentityService(IdentityTestHarness.Create(context).UserManager);

        Assert.Null(await service.GetUserAsync(999));
    }

    [Fact]
    public async Task GetUsersBulkAsync_ReturnsOnlyRequestedUsers()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var userManager = IdentityTestHarness.Create(context).UserManager;
        var first = new AppUser { UserName = "first", Email = "first@example.com", IsActive = true };
        var second = new AppUser { UserName = "second", Email = "second@example.com", IsActive = true };
        await userManager.CreateAsync(first, "Password1!");
        await userManager.CreateAsync(second, "Password1!");
        var service = new UserIdentityService(userManager);

        var result = await service.GetUsersBulkAsync([first.Id, second.Id, 999]);

        Assert.Equal(2, result.Count);
        Assert.Equal("first", result[first.Id].Username);
        Assert.Equal("second", result[second.Id].Username);
    }

    [Fact]
    public async Task GetRolesAsync_ReturnsAssignedRoles()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var identity = IdentityTestHarness.Create(context);
        var userManager = identity.UserManager;
        var roleManager = identity.RoleManager;
        await roleManager.CreateAsync(new AppRole { Name = "Student" });
        var user = new AppUser { UserName = "jdoe", Email = "jdoe@example.com", IsActive = true };
        await userManager.CreateAsync(user, "Password1!");
        await userManager.AddToRoleAsync(user, "Student");
        var service = new UserIdentityService(userManager);

        var roles = await service.GetRolesAsync(user.Id);

        Assert.Equal(["Student"], roles);
    }
}
