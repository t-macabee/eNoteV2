using eNote.Application.Constants;
using eNote.Application.Features.Identity.Users;
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
        var service = new UserIdentityService(userManager, context);

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
        var service = new UserIdentityService(IdentityTestHarness.Create(context).UserManager, context);

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
        var service = new UserIdentityService(userManager, context);

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
        var service = new UserIdentityService(userManager, context);

        var roles = await service.GetRolesAsync(user.Id);

        Assert.Equal(["Student"], roles);
    }

    [Fact]
    public async Task FindUserIdsAsync_FiltersByNameAndActiveState()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var userManager = IdentityTestHarness.Create(context).UserManager;

        var jane = new AppUser { UserName = "jdoe", Email = "jdoe@example.com", FirstName = "Jane", LastName = "Doe", IsActive = true };
        var alice = new AppUser { UserName = "asmith", Email = "asmith@example.com", FirstName = "Alice", LastName = "Smith", IsActive = false };
        var bob = new AppUser { UserName = "bbrown", Email = "bbrown@example.com", FirstName = "Bob", LastName = "Brown", IsActive = true };
        var nullName = new AppUser { UserName = "noname", Email = "noname@example.com", FirstName = null, LastName = null, IsActive = true };

        await userManager.CreateAsync(jane, "Password1!");
        await userManager.CreateAsync(alice, "Password1!");
        await userManager.CreateAsync(bob, "Password1!");
        await userManager.CreateAsync(nullName, "Password1!");

        var service = new UserIdentityService(userManager, context);

        var firstMatch = await service.FindUserIdsAsync("jane", null);
        Assert.Equal([jane.Id], firstMatch);

        var lastMatch = await service.FindUserIdsAsync("SMITH", null);
        Assert.Equal([alice.Id], lastMatch);

        var userMatch = await service.FindUserIdsAsync("bbrown", null);
        Assert.Equal([bob.Id], userMatch);

        var fullMatch = await service.FindUserIdsAsync("Jane Doe", null);
        Assert.Equal([jane.Id], fullMatch);

        var activeOnly = await service.FindUserIdsAsync(null, true);
        Assert.Contains(jane.Id, activeOnly);
        Assert.Contains(bob.Id, activeOnly);
        Assert.Contains(nullName.Id, activeOnly);
        Assert.DoesNotContain(alice.Id, activeOnly);

        var inactiveOnly = await service.FindUserIdsAsync(null, false);
        Assert.Equal([alice.Id], inactiveOnly);

        var nameAndInactive = await service.FindUserIdsAsync("smith", false);
        Assert.Equal([alice.Id], nameAndInactive);

        var nameAndActiveMismatch = await service.FindUserIdsAsync("smith", true);
        Assert.Empty(nameAndActiveMismatch);
    }

    [Fact]
    public async Task SearchAsync_ReturnsExactTotalCount_ForLargeUserSet()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var users = Enumerable.Range(1, 150)
            .Select(i => new AppUser
            {
                UserName = $"user{i:000}",
                Email = $"user{i:000}@example.com",
                FirstName = "First",
                LastName = $"Last{i:000}",
                IsActive = true
            })
            .ToList();
        context.Set<AppUser>().AddRange(users);
        await context.SaveChangesAsync();
        var service = new UserIdentityService(IdentityTestHarness.Create(context).UserManager, context);

        var result = await service.SearchAsync(new AdminUserSearchObject { Page = 2, PageSize = 20 });

        Assert.Equal(150, result.TotalCount);
        Assert.Equal(20, result.Items.Count);
        Assert.Equal(2, result.Page);
    }

    [Fact]
    public async Task SearchAsync_FiltersByRole_IncludingAdministrator()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var identity = IdentityTestHarness.Create(context);
        var userManager = identity.UserManager;
        string[] roles = [AppRoles.Administrator, AppRoles.Instructor, AppRoles.Student, AppRoles.StoreEmployee];

        foreach (var role in roles)
        {
            await identity.RoleManager.CreateAsync(new AppRole { Name = role });
        }

        var userIds = new Dictionary<string, int>();

        foreach (var role in roles)
        {
            var user = new AppUser { UserName = role.ToLower(), Email = $"{role.ToLower()}@example.com", IsActive = true };
            await userManager.CreateAsync(user, "Password1!");
            await userManager.AddToRoleAsync(user, role);
            userIds[role] = user.Id;
        }

        var service = new UserIdentityService(userManager, context);

        foreach (var role in roles)
        {
            var result = await service.SearchAsync(new AdminUserSearchObject { Role = role });

            var item = Assert.Single(result.Items);
            Assert.Equal(userIds[role], item.AppUserId);
            Assert.Equal(role, item.Role);
        }
    }

    [Fact]
    public async Task SearchAsync_FiltersByNameAndActiveState()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var userManager = IdentityTestHarness.Create(context).UserManager;

        var jane = new AppUser { UserName = "jdoe", Email = "jdoe@example.com", FirstName = "Jane", LastName = "Doe", IsActive = true };
        var alice = new AppUser { UserName = "asmith", Email = "asmith@example.com", FirstName = "Alice", LastName = "Smith", IsActive = false };

        await userManager.CreateAsync(jane, "Password1!");
        await userManager.CreateAsync(alice, "Password1!");

        var service = new UserIdentityService(userManager, context);

        var byName = await service.SearchAsync(new AdminUserSearchObject { Name = "jane" });
        Assert.Equal([jane.Id], byName.Items.Select(x => x.AppUserId));

        var byActive = await service.SearchAsync(new AdminUserSearchObject { IsActive = false });
        Assert.Equal([alice.Id], byActive.Items.Select(x => x.AppUserId));

        var byNameAndActive = await service.SearchAsync(new AdminUserSearchObject { Name = "smith", IsActive = true });
        Assert.Empty(byNameAndActive.Items);
    }

    [Fact]
    public async Task SearchAsync_LoadsStudentAndEmployeeProfileRows()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var userManager = IdentityTestHarness.Create(context).UserManager;

        var studentUser = new AppUser { UserName = "student", Email = "student@example.com", IsActive = true };
        var employeeUser = new AppUser { UserName = "employee", Email = "employee@example.com", IsActive = true };
        await userManager.CreateAsync(studentUser, "Password1!");
        await userManager.CreateAsync(employeeUser, "Password1!");

        var store = new MusicStore("Music Shop", "09-17");
        context.Set<MusicStore>().Add(store);
        await context.SaveChangesAsync();

        var student = new Student(studentUser.Id, new DateTime(2026, 1, 10));
        student.UpdateMembership(new DateTime(2026, 12, 31));
        context.Set<Student>().Add(student);
        context.Set<MusicStoreEmployee>().Add(new MusicStoreEmployee(employeeUser.Id, store.Id, isManager: true));
        await context.SaveChangesAsync();

        var service = new UserIdentityService(userManager, context);

        var result = await service.SearchAsync(new AdminUserSearchObject());

        var studentItem = result.Items.Single(x => x.AppUserId == studentUser.Id);
        Assert.Equal(student.Id, studentItem.StudentId);
        Assert.Equal(student.EnrollmentDate, studentItem.EnrollmentDate);
        Assert.Equal(student.MembershipPaidUntil, studentItem.MembershipPaidUntil);

        var employeeItem = result.Items.Single(x => x.AppUserId == employeeUser.Id);
        Assert.Equal("Music Shop", employeeItem.StoreName);
        Assert.True(employeeItem.IsManager);
    }
}
