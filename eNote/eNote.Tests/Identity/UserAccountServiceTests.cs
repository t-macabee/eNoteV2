using eNote.Application.Common.Localization;
using eNote.Infrastructure.Identity;
using eNote.Tests.TestUtils;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace eNote.Tests.Identity;

public sealed class UserAccountServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateUserAsync_CreatesActiveUser()
    {
        var harness = await CreateHarnessAsync();
        var service = harness.Service;

        var result = await service.CreateUserAsync("jdoe", "jdoe@example.com", "Password1!", "Jane", "Doe");

        Assert.NotNull(result.UserId);
        Assert.Null(result.Error);
        var user = await harness.UserManager.FindByIdAsync(result.UserId!.Value.ToString());
        Assert.NotNull(user);
        Assert.True(user.IsActive);
        Assert.True(user.EmailConfirmed);
        Assert.Equal("Jane", user.FirstName);
    }

    [Fact]
    public async Task CreateUserAsync_ReturnsError_WhenUsernameTaken()
    {
        var harness = await CreateHarnessAsync();
        var service = harness.Service;
        await service.CreateUserAsync("jdoe", "jdoe@example.com", "Password1!", null, null);

        var result = await service.CreateUserAsync("jdoe", "other@example.com", "Password1!", null, null);

        Assert.Null(result.UserId);
        Assert.Equal(Messages.UsernameTaken, result.Error);
    }

    [Fact]
    public async Task CreateUserAsync_ReturnsError_WhenEmailTaken()
    {
        var harness = await CreateHarnessAsync();
        var service = harness.Service;
        await service.CreateUserAsync("jdoe", "jdoe@example.com", "Password1!", null, null);

        var result = await service.CreateUserAsync("other", "jdoe@example.com", "Password1!", null, null);

        Assert.Null(result.UserId);
        Assert.Equal(Messages.EmailTaken, result.Error);
    }

    [Fact]
    public async Task AssignSingleRoleAsync_SwapsExistingRole()
    {
        var harness = await CreateHarnessAsync();
        var service = harness.Service;
        await service.CreateUserAsync("jdoe", "jdoe@example.com", "Password1!", null, null);
        var userId = (await harness.UserManager.FindByNameAsync("jdoe"))!.Id;
        await service.AssignSingleRoleAsync(userId, "Student");
        await harness.RoleManager.CreateAsync(new AppRole { Name = "Instructor" });

        var result = await service.AssignSingleRoleAsync(userId, "Instructor");

        Assert.True(result.Success);
        var roles = await harness.UserManager.GetRolesAsync((await harness.UserManager.FindByIdAsync(userId.ToString()))!); Assert.Equal(["Instructor"], roles);
    }

    [Fact]
    public async Task AssignSingleRoleAsync_ReturnsError_ForMissingUser()
    {
        var harness = await CreateHarnessAsync();
        var result = await harness.Service.AssignSingleRoleAsync(999, "Student");

        Assert.False(result.Success);
        Assert.Equal(Messages.NotFound, result.Error);
    }

    [Fact]
    public async Task UpdateExistingUserAsync_UpdatesFields()
    {
        var harness = await CreateHarnessAsync();
        var service = harness.Service;
        await service.CreateUserAsync("jdoe", "jdoe@example.com", "Password1!", "Jane", "Doe");
        var userId = (await harness.UserManager.FindByNameAsync("jdoe"))!.Id;

        var result = await service.UpdateExistingUserAsync(userId, "new@example.com", "Janet", "Smith");

        Assert.True(result.Success);
        var user = (await harness.UserManager.FindByIdAsync(userId.ToString()))!;
        Assert.Equal("new@example.com", user.Email);
        Assert.Equal("Janet", user.FirstName);
        Assert.Equal("Smith", user.LastName);
    }

    [Fact]
    public async Task UpdatePictureAsync_SetsPath_AndDeletesOldPicture()
    {
        var harness = await CreateHarnessAsync();
        var service = harness.Service;
        await service.CreateUserAsync("jdoe", "jdoe@example.com", "Password1!", null, null);
        var user = (await harness.UserManager.FindByNameAsync("jdoe"))!;
        user.PicturePath = "/api/uploads/profile-pictures/old.png";
        await harness.UserManager.UpdateAsync(user);

        var result = await service.UpdatePictureAsync(user.Id, PngStream(), "picture.png", "image/png");

        Assert.True(result.Success);
        var updated = (await harness.UserManager.FindByIdAsync(user.Id.ToString()))!;
        Assert.StartsWith("/api/uploads/profile-pictures/", updated.PicturePath);
        Assert.Contains("/api/uploads/profile-pictures/old.png", harness.FileStorage.DeletedPaths);
    }

    [Fact]
    public async Task GetPictureAsync_ReturnsStoredStream()
    {
        var harness = await CreateHarnessAsync();
        var service = harness.Service;
        await service.CreateUserAsync("jdoe", "jdoe@example.com", "Password1!", null, null);
        var user = (await harness.UserManager.FindByNameAsync("jdoe"))!;
        user.PicturePath = "/api/uploads/profile-pictures/pic.png";
        await harness.UserManager.UpdateAsync(user);
        using var stored = new MemoryStream([1, 2, 3]);
        harness.FileStorage.OpenReadResult = (stored, "image/png");

        var (data, contentType) = await service.GetPictureAsync(user.Id);

        Assert.Same(stored, data);
        Assert.Equal("image/png", contentType);
        Assert.Equal("/api/uploads/profile-pictures/pic.png", harness.FileStorage.OpenReadCalls.Single());
    }

    [Fact]
    public async Task DeletePictureAsync_ClearsPath_AndDeletesFile()
    {
        var harness = await CreateHarnessAsync();
        var service = harness.Service;
        await service.CreateUserAsync("jdoe", "jdoe@example.com", "Password1!", null, null);
        var user = (await harness.UserManager.FindByNameAsync("jdoe"))!;
        user.PicturePath = "/api/uploads/profile-pictures/pic.png";
        await harness.UserManager.UpdateAsync(user);

        var result = await service.DeletePictureAsync(user.Id);

        Assert.True(result.Success);
        var updated = (await harness.UserManager.FindByIdAsync(user.Id.ToString()))!;
        Assert.Null(updated.PicturePath);
        Assert.Contains("/api/uploads/profile-pictures/pic.png", harness.FileStorage.DeletedPaths);
    }

    [Fact]
    public async Task ChangePasswordAsync_ChangesPassword()
    {
        var harness = await CreateHarnessAsync();
        var service = harness.Service;
        await service.CreateUserAsync("jdoe", "jdoe@example.com", "Password1!", null, null);
        var userId = (await harness.UserManager.FindByNameAsync("jdoe"))!.Id;

        var result = await service.ChangePasswordAsync(userId, "Password1!", "Newpassword1!");

        Assert.True(result.Success);
        var user = (await harness.UserManager.FindByIdAsync(userId.ToString()))!;
        Assert.True(await harness.UserManager.CheckPasswordAsync(user, "Newpassword1!"));
    }

    private static async Task<Harness> CreateHarnessAsync()
    {
        var context = TestDbContextFactory.CreateContext(Now);
        var identity = IdentityTestHarness.Create(context);
        await identity.RoleManager.CreateAsync(new AppRole { Name = "Student" });
        var fileStorage = new RecordingFileStorageService();
        return new Harness(context, identity.UserManager, identity.RoleManager, fileStorage, new UserAccountService(identity.UserManager, fileStorage));
    }

    private static MemoryStream PngStream() =>
        new([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]);

    private sealed class Harness(
        ENoteContext context,
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager,
        RecordingFileStorageService fileStorage,
        UserAccountService service)
    {
        public ENoteContext Context => context;
        public UserManager<AppUser> UserManager => userManager;
        public RoleManager<AppRole> RoleManager => roleManager;
        public RecordingFileStorageService FileStorage => fileStorage;
        public UserAccountService Service => service;
    }
}
