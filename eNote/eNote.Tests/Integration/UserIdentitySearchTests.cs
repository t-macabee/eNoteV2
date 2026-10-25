using eNote.Application.Features.Identity.Users;
using eNote.Infrastructure.Identity;
using eNote.Tests.TestUtils;
using Microsoft.AspNetCore.Identity;

namespace eNote.Tests.Integration;

[Collection(SqlServerCollection.Name)]
public sealed class UserIdentitySearchTests(SqlServerFixture fixture)
{
    [Fact]
    public async Task SearchAsync_OrdersByRoleThenLastNameAndFiltersByRole()
    {
        var database = await fixture.CreateDatabaseAsync();
        await using var context = database.CreateContext();
        var identity = IdentityTestHarness.Create(context);
        var userManager = identity.UserManager;

        foreach (var role in new[] { "Student", "StoreEmployee", "Instructor", "Administrator" })
        {
            await identity.RoleManager.CreateAsync(new AppRole { Name = role });
        }

        await CreateUserAsync(userManager, "student", "Zoe", "Zimmer", "Student");
        await CreateUserAsync(userManager, "employee", "Karl", "Kraus", "StoreEmployee");
        await CreateUserAsync(userManager, "instructor2", "Anna", "Zeller", "Instructor");
        await CreateUserAsync(userManager, "instructor1", "Bruno", "Adler", "Instructor");
        await CreateUserAsync(userManager, "admin", "Ada", "Admin", "Administrator");

        var service = new UserIdentityService(userManager, context);

        var all = await service.SearchAsync(new AdminUserSearchObject { PageSize = 10 });

        Assert.Equal(5, all.TotalCount);
        Assert.Equal(
            ["Administrator", "Instructor", "Instructor", "StoreEmployee", "Student"],
            all.Items.Select(x => x.Role));
        Assert.Equal(
            ["Ada Admin", "Bruno Adler", "Anna Zeller", "Karl Kraus", "Zoe Zimmer"],
            all.Items.Select(x => $"{x.FirstName} {x.LastName}"));

        var administrators = await service.SearchAsync(new AdminUserSearchObject
        {
            Role = "Administrator"
        });

        var administrator = Assert.Single(administrators.Items);
        Assert.Equal("admin", administrator.Username);
    }

    private static async Task CreateUserAsync(
        UserManager<AppUser> userManager,
        string username,
        string firstName,
        string lastName,
        string role)
    {
        var user = new AppUser
        {
            UserName = username,
            Email = $"{username}@example.com",
            FirstName = firstName,
            LastName = lastName,
            IsActive = true
        };

        await userManager.CreateAsync(user, "Password1!");
        await userManager.AddToRoleAsync(user, role);
    }
}
