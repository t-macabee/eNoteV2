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
    public async Task CreateUserAsync_TrimsAllFields()
    {
        var harness = await CreateHarnessAsync();
        var service = harness.Service;

        var userId = await service.CreateUserAsync("  jdoe  ", "  jdoe@example.com  ", "Password1!", "  Jane  ", "  Doe  ");

        var user = await harness.UserManager.FindByIdAsync(userId.ToString());
        Assert.NotNull(user);
        Assert.Equal("jdoe", user.UserName);
        Assert.Equal("jdoe@example.com", user.Email);
        Assert.Equal("Jane", user.FirstName);
        Assert.Equal("Doe", user.LastName);
    }

    [Fact]
    public async Task CreateUserAsync_ThrowsConflict_WhenPaddedUsernameTaken()
    {
        var harness = await CreateHarnessAsync();
        var service = harness.Service;
        await service.CreateUserAsync("jdoe", "jdoe@example.com", "Password1!", null, null);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateUserAsync("  jdoe  ", "other@example.com", "Password1!", null, null));

        Assert.Equal(Messages.UsernameTaken, exception.Message);
    }

    [Fact]
    public async Task CreateUserAsync_ThrowsConflict_WhenPaddedEmailTaken()
    {
        var harness = await CreateHarnessAsync();
        var service = harness.Service;
        await service.CreateUserAsync("jdoe", "jdoe@example.com", "Password1!", null, null);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateUserAsync("other", "  jdoe@example.com  ", "Password1!", null, null));

        Assert.Equal(Messages.EmailTaken, exception.Message);
    }

    [Fact]
    public async Task AddRoleAsync_AddsTheRole()
    {
        var harness = await CreateHarnessAsync();
        var service = harness.Service;
        await service.CreateUserAsync("jdoe", "jdoe@example.com", "Password1!", null, null);
        var userId = (await harness.UserManager.FindByNameAsync("jdoe"))!.Id;
        await harness.RoleManager.CreateAsync(new AppRole { Name = "Student" });

        await service.AddRoleAsync(userId, "Student");

        var roles = await harness.UserManager.GetRolesAsync((await harness.UserManager.FindByIdAsync(userId.ToString()))!);
        Assert.Equal(["Student"], roles);
    }

    [Fact]
    public async Task AddRoleAsync_ThrowsBusinessException_ForUnknownRole()
    {
        var harness = await CreateHarnessAsync();
        var service = harness.Service;
        await service.CreateUserAsync("jdoe", "jdoe@example.com", "Password1!", null, null);
        var userId = (await harness.UserManager.FindByNameAsync("jdoe"))!.Id;

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            service.AddRoleAsync(userId, "NonExistentRole"));

        Assert.Contains("Greška pri dodjeli uloge NonExistentRole korisniku jdoe", exception.Message);
    }

    [Fact]
    public async Task AddRoleAsync_ThrowsNotFound_ForMissingUser()
    {
        var harness = await CreateHarnessAsync();

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            harness.Service.AddRoleAsync(999, "Student"));

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
    public async Task UpdateExistingUserAsync_KeepsNames_WhenNullPassed()
    {
        var harness = await CreateHarnessAsync();
        var service = harness.Service;
        await service.CreateUserAsync("jdoe", "jdoe@example.com", "Password1!", "Jane", "Doe");
        var userId = (await harness.UserManager.FindByNameAsync("jdoe"))!.Id;

        await service.UpdateExistingUserAsync(userId, "jdoe@example.com", null, null);

        var user = (await harness.UserManager.FindByIdAsync(userId.ToString()))!;
        Assert.Equal("Jane", user.FirstName);
        Assert.Equal("Doe", user.LastName);
    }

    [Fact]
    public async Task UpdateExistingUserAsync_TrimsEmail()
    {
        var harness = await CreateHarnessAsync();
        var service = harness.Service;
        await service.CreateUserAsync("jdoe", "jdoe@example.com", "Password1!", null, null);
        var userId = (await harness.UserManager.FindByNameAsync("jdoe"))!.Id;

        await service.UpdateExistingUserAsync(userId, "  new@example.com  ", null, null);

        var user = (await harness.UserManager.FindByIdAsync(userId.ToString()))!;
        Assert.Equal("new@example.com", user.Email);
    }

    [Fact]
    public async Task UpdateExistingUserAsync_KeepsSameEmail()
    {
        var harness = await CreateHarnessAsync();
        var service = harness.Service;
        await service.CreateUserAsync("jdoe", "jdoe@example.com", "Password1!", null, null);
        var userId = (await harness.UserManager.FindByNameAsync("jdoe"))!.Id;

        await service.UpdateExistingUserAsync(userId, "jdoe@example.com", "Jane", null);

        var user = (await harness.UserManager.FindByIdAsync(userId.ToString()))!;
        Assert.Equal("jdoe@example.com", user.Email);
        Assert.Equal("Jane", user.FirstName);
    }

    [Fact]
    public async Task UpdateExistingUserAsync_ThrowsConflict_WhenEmailBelongsToAnotherUser()
    {
        var harness = await CreateHarnessAsync();
        var service = harness.Service;
        await service.CreateUserAsync("jdoe", "jdoe@example.com", "Password1!", null, null);
        await service.CreateUserAsync("other", "other@example.com", "Password1!", null, null);
        var otherId = (await harness.UserManager.FindByNameAsync("other"))!.Id;

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            service.UpdateExistingUserAsync(otherId, "jdoe@example.com", null, null));

        Assert.Equal(Messages.EmailTaken, exception.Message);
    }

    [Fact]
    public async Task UpdateExistingUserAsync_StoresDateOfBirthAtMidnight()
    {
        var harness = await CreateHarnessAsync();
        var service = harness.Service;
        await service.CreateUserAsync("jdoe", "jdoe@example.com", "Password1!", null, null);
        var userId = (await harness.UserManager.FindByNameAsync("jdoe"))!.Id;

        await service.UpdateExistingUserAsync(userId, "jdoe@example.com", null, null, new DateTime(2001, 3, 4, 15, 30, 0));

        var user = (await harness.UserManager.FindByIdAsync(userId.ToString()))!;
        Assert.Equal(new DateTime(2001, 3, 4), user.DateOfBirth);
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
    public async Task GetPictureAsync_ReturnsNulls_ForUnknownUser()
    {
        var harness = await CreateHarnessAsync();

        var (data, contentType) = await harness.Service.GetPictureAsync(999);

        Assert.Null(data);
        Assert.Null(contentType);
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
    public async Task UpdatePictureAsync_RollsBackNewPicture_WhenUpdateFails()
    {
        var harness = await CreateHarnessAsync();
        var service = harness.Service;
        await service.CreateUserAsync("userA", "a@example.com", "Password1!", null, null);
        await service.CreateUserAsync("userB", "b@example.com", "Password1!", null, null);
        var userA = (await harness.UserManager.FindByNameAsync("userA"))!;
        var userB = (await harness.UserManager.FindByNameAsync("userB"))!;
        userA.PicturePath = "/api/uploads/profile-pictures/old.png";
        await harness.UserManager.UpdateAsync(userA);

        var trackedA = (await harness.UserManager.FindByIdAsync(userA.Id.ToString()))!;
        trackedA.UserName = userB.UserName;

        await Assert.ThrowsAsync<BusinessException>(() =>
            service.UpdatePictureAsync(userA.Id, PngStream(), "image/png"));

        Assert.Contains(harness.FileStorage.DeletedPaths, p => p.Contains("profile-pictures") && !p.Contains("old.png"));
        Assert.DoesNotContain("/api/uploads/profile-pictures/old.png", harness.FileStorage.DeletedPaths);
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
    public async Task VerifyPasswordAsync_ReturnsFalse_ForClosedAccount()
    {
        var harness = await CreateHarnessAsync();
        var service = harness.Service;
        await service.CreateUserAsync("jdoe", "jdoe@example.com", "Password1!", null, null);
        var userId = (await harness.UserManager.FindByNameAsync("jdoe"))!.Id;

        await service.CloseAccountAsync(userId);

        Assert.False(await service.VerifyPasswordAsync(userId, "Password1!"));
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
        Assert.Null(await harness.UserManager.FindByNameAsync("jdoe"));
        Assert.Null(await harness.UserManager.FindByEmailAsync("jdoe@example.com"));

        var newUserId = await service.CreateUserAsync("jdoe", "jdoe@example.com", "Password1!", "Jane", "Doe");
        Assert.True(newUserId > 0);
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
