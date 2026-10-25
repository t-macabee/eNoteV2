using eNote.Application.Common.Files;
using eNote.Application.Common.Localization;
using eNote.Infrastructure.Identity;
using eNote.Tests.TestUtils;
using Microsoft.AspNetCore.Identity;

namespace eNote.Tests.Identity;

public sealed class UserAccountServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateUserAsync_CreatesActiveUser()
    {
        var harness = await CreateHarnessAsync();
        var service = harness.Service;

        var userId = await service.CreateUserAsync("jdoe", "jdoe@example.com", "Password1!", "Jane", "Doe");

        var user = await harness.UserManager.FindByIdAsync(userId.ToString());
        Assert.NotNull(user);
        Assert.True(user.IsActive);
        Assert.True(user.EmailConfirmed);
        Assert.Equal("Jane", user.FirstName);
    }

    [Fact]
    public async Task CreateUserAsync_ThrowsConflict_WhenUsernameTaken()
    {
        var harness = await CreateHarnessAsync();
        var service = harness.Service;
        await service.CreateUserAsync("jdoe", "jdoe@example.com", "Password1!", null, null);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateUserAsync("jdoe", "other@example.com", "Password1!", null, null));

        Assert.Equal(Messages.UsernameTaken, exception.Message);
    }

    [Fact]
    public async Task CreateUserAsync_ThrowsConflict_WhenEmailTaken()
    {
        var harness = await CreateHarnessAsync();
        var service = harness.Service;
        await service.CreateUserAsync("jdoe", "jdoe@example.com", "Password1!", null, null);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateUserAsync("other", "jdoe@example.com", "Password1!", null, null));

        Assert.Equal(Messages.EmailTaken, exception.Message);
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

        await service.AssignSingleRoleAsync(userId, "Instructor");

        var roles = await harness.UserManager.GetRolesAsync((await harness.UserManager.FindByIdAsync(userId.ToString()))!); Assert.Equal(["Instructor"], roles);
    }

    [Fact]
    public async Task AssignSingleRoleAsync_ThrowsNotFound_ForMissingUser()
    {
        var harness = await CreateHarnessAsync();

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            harness.Service.AssignSingleRoleAsync(999, "Student"));

        Assert.Equal(Messages.NotFound, exception.Message);
    }

    [Fact]
    public async Task UpdateExistingUserAsync_UpdatesFields()
    {
        var harness = await CreateHarnessAsync();
        var service = harness.Service;
        await service.CreateUserAsync("jdoe", "jdoe@example.com", "Password1!", "Jane", "Doe");
        var userId = (await harness.UserManager.FindByNameAsync("jdoe"))!.Id;
        var existing = (await harness.UserManager.FindByIdAsync(userId.ToString()))!;
        existing.IsActive = false;
        await harness.UserManager.UpdateAsync(existing);

        await service.UpdateExistingUserAsync(userId, "new@example.com", "Janet", "Smith");

        var user = (await harness.UserManager.FindByIdAsync(userId.ToString()))!;
        Assert.Equal("new@example.com", user.Email);
        Assert.Equal("Janet", user.FirstName);
        Assert.Equal("Smith", user.LastName);
        Assert.False(user.IsActive);
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

        await service.UpdatePictureAsync(user.Id, PngStream(), "image/png");

        var updated = (await harness.UserManager.FindByIdAsync(user.Id.ToString()))!;
        Assert.StartsWith("/api/v1/uploads/profile-pictures/", updated.PicturePath);
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
    public async Task GetPictureAsync_ReturnsDefaultPicture_WhenUserHasNoPicture()
    {
        var harness = await CreateHarnessAsync();
        var service = harness.Service;
        await service.CreateUserAsync("jdoe", "jdoe@example.com", "Password1!", null, null);
        var user = (await harness.UserManager.FindByNameAsync("jdoe"))!;
        using var defaultPicture = new MemoryStream([1, 2, 3]);
        harness.FileStorage.OpenReadResult = (defaultPicture, "image/webp");

        var (data, contentType) = await service.GetPictureAsync(user.Id);

        Assert.Same(defaultPicture, data);
        Assert.Equal("image/webp", contentType);
        Assert.Equal(DefaultImages.UserPicture, harness.FileStorage.OpenReadCalls.Single());

        var identity = await new UserIdentityService(harness.UserManager, harness.Context).GetUserAsync(user.Id);
        Assert.False(identity!.HasPicture);
        Assert.Null((await harness.UserManager.FindByIdAsync(user.Id.ToString()))!.PicturePath);
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

        await service.DeletePictureAsync(user.Id);

        var updated = (await harness.UserManager.FindByIdAsync(user.Id.ToString()))!;
        Assert.Null(updated.PicturePath);
        Assert.Contains("/api/uploads/profile-pictures/pic.png", harness.FileStorage.DeletedPaths);
    }

    [Fact]
    public async Task DeleteUserAsync_RemovesUser_AndReturnsPicturePathWithoutDeletingFile()
    {
        var harness = await CreateHarnessAsync();
        var service = harness.Service;
        await service.CreateUserAsync("jdoe", "jdoe@example.com", "Password1!", null, null);
        var user = (await harness.UserManager.FindByNameAsync("jdoe"))!;
        user.PicturePath = "/api/uploads/profile-pictures/pic.png";
        await harness.UserManager.UpdateAsync(user);

        var picturePath = await service.DeleteUserAsync(user.Id);

        Assert.Equal("/api/uploads/profile-pictures/pic.png", picturePath);
        Assert.Null(await harness.UserManager.FindByIdAsync(user.Id.ToString()));
        Assert.Empty(harness.FileStorage.DeletedPaths);
    }

    [Fact]
    public async Task ChangePasswordAsync_ChangesPassword()
    {
        var harness = await CreateHarnessAsync();
        var service = harness.Service;
        await service.CreateUserAsync("jdoe", "jdoe@example.com", "Password1!", null, null);
        var userId = (await harness.UserManager.FindByNameAsync("jdoe"))!.Id;

        var stampBefore = (await harness.UserManager.FindByIdAsync(userId.ToString()))!.SecurityStamp;

        await service.ChangePasswordAsync(userId, "Password1!", "Newpassword1!");

        var user = (await harness.UserManager.FindByIdAsync(userId.ToString()))!;
        Assert.True(await harness.UserManager.CheckPasswordAsync(user, "Newpassword1!"));
        Assert.NotEqual(stampBefore, user.SecurityStamp);
    }

    [Fact]
    public async Task VerifyPasswordAsync_ChecksTheStoredPassword()
    {
        var harness = await CreateHarnessAsync();
        var service = harness.Service;
        await service.CreateUserAsync("jdoe", "jdoe@example.com", "Password1!", null, null);
        var userId = (await harness.UserManager.FindByNameAsync("jdoe"))!.Id;

        Assert.True(await service.VerifyPasswordAsync(userId, "Password1!"));
        Assert.False(await service.VerifyPasswordAsync(userId, "Wrong1!"));
    }

    [Fact]
    public async Task VerifyPasswordAsync_ReturnsFalse_ForMissingUser()
    {
        var harness = await CreateHarnessAsync();

        Assert.False(await harness.Service.VerifyPasswordAsync(999, "Password1!"));
    }

    [Fact]
    public async Task CloseAccountAsync_AnonymizesAndDeactivatesUser()
    {
        var harness = await CreateHarnessAsync();
        var service = harness.Service;
        await service.CreateUserAsync("jdoe", "jdoe@example.com", "Password1!", "Jane", "Doe");
        var user = (await harness.UserManager.FindByNameAsync("jdoe"))!;
        user.PhoneNumber = "061111222";
        user.DateOfBirth = new DateTime(2001, 3, 4);
        user.PicturePath = "/api/uploads/profile-pictures/jdoe.png";
        await harness.UserManager.UpdateAsync(user);
        var stampBefore = user.SecurityStamp;

        var picturePath = await service.CloseAccountAsync(user.Id);

        Assert.Equal("/api/uploads/profile-pictures/jdoe.png", picturePath);
        var closed = (await harness.UserManager.FindByIdAsync(user.Id.ToString()))!;
        Assert.Equal("Obrisani", closed.FirstName);
        Assert.Equal("korisnik", closed.LastName);
        Assert.Equal($"obrisan-{closed.Id}", closed.UserName);
        Assert.Equal($"obrisan-{closed.Id}@enote.invalid", closed.Email);
        Assert.Null(closed.PhoneNumber);
        Assert.Null(closed.DateOfBirth);
        Assert.Null(closed.PasswordHash);
        Assert.Null(closed.PicturePath);
        Assert.False(closed.IsActive);
        Assert.NotEqual(stampBefore, closed.SecurityStamp);
        Assert.Empty(harness.FileStorage.DeletedPaths);
        Assert.False(await harness.UserManager.CheckPasswordAsync(closed, "Password1!"));
    }

    [Fact]
    public async Task CloseAccountAsync_ThrowsNotFound_ForMissingUser()
    {
        var harness = await CreateHarnessAsync();

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            harness.Service.CloseAccountAsync(999));

        Assert.Equal(Messages.NotFound, exception.Message);
    }

    [Fact]
    public async Task SetActiveAsync_DeactivatesUser()
    {
        var harness = await CreateHarnessAsync();
        var service = harness.Service;
        await service.CreateUserAsync("jdoe", "jdoe@example.com", "Password1!", null, null);
        var userId = (await harness.UserManager.FindByNameAsync("jdoe"))!.Id;

        var stampBefore = (await harness.UserManager.FindByIdAsync(userId.ToString()))!.SecurityStamp;

        await service.SetActiveAsync(userId, false);

        var user = (await harness.UserManager.FindByIdAsync(userId.ToString()))!;
        Assert.False(user.IsActive);
        Assert.NotEqual(stampBefore, user.SecurityStamp);
    }

    [Fact]
    public async Task SetActiveAsync_ThrowsNotFound_ForMissingUser()
    {
        var harness = await CreateHarnessAsync();

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            harness.Service.SetActiveAsync(999, false));

        Assert.Equal(Messages.NotFound, exception.Message);
    }

    private static async Task<Harness> CreateHarnessAsync()
    {
        var context = TestDbContextFactory.CreateContext(Now);
        var identity = IdentityTestHarness.Create(context);
        await identity.RoleManager.CreateAsync(new AppRole { Name = "Student" });
        var fileStorage = new RecordingFileStorageService();
        return new Harness(identity.UserManager, identity.RoleManager, fileStorage, context, new UserAccountService(identity.UserManager, fileStorage));
    }

    private static MemoryStream PngStream() =>
        new([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]);

    private sealed class Harness(
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager,
        RecordingFileStorageService fileStorage,
        ENoteContext context,
        UserAccountService service)
    {
        public UserManager<AppUser> UserManager => userManager;
        public RoleManager<AppRole> RoleManager => roleManager;
        public RecordingFileStorageService FileStorage => fileStorage;
        public ENoteContext Context => context;
        public UserAccountService Service => service;
    }
}
