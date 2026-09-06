using eNote.Application.Features.Identity.Users;
using eNote.Application.Features.Identity.Users.Services;

namespace eNote.Tests.Identity;

public sealed class UserSelfServiceTests
{
    [Fact]
    public async Task UpdateProfileAsync_UsesCurrentUserId()
    {
        var account = new RecordingUserAccountService();
        var service = new UserSelfService(account, new TestCurrentUserService(42));
        var request = new UpdateProfileRequest
        {
            Email = "new@example.com",
            FirstName = "New",
            LastName = "User",
            DateOfBirth = new DateTime(2000, 1, 2)
        };

        await service.UpdateProfileAsync(request);

        Assert.Equal(42, account.UpdatedUserId);
        Assert.Equal("new@example.com", account.UpdatedEmail);
        Assert.Equal(new DateTime(2000, 1, 2), account.UpdatedDateOfBirth);
    }

    [Fact]
    public async Task ChangePasswordAsync_UsesCurrentUserIdAndPasswordValues()
    {
        var account = new RecordingUserAccountService();
        var service = new UserSelfService(account, new TestCurrentUserService(7));
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "old",
            NewPassword = "new",
            ConfirmNewPassword = "new"
        };

        await service.ChangePasswordAsync(request);

        Assert.Equal(7, account.PasswordUserId);
        Assert.Equal("old", account.CurrentPassword);
        Assert.Equal("new", account.NewPassword);
    }

    [Fact]
    public async Task PictureMethods_UseCurrentUserId()
    {
        var account = new RecordingUserAccountService();
        var service = new UserSelfService(account, new TestCurrentUserService(11));
        await using var picture = new MemoryStream([1, 2, 3]);

        await service.UpdatePictureAsync(picture, "picture.png", "image/png");
        await service.GetPictureAsync();
        await service.DeletePictureAsync();

        Assert.Equal(11, account.PictureUpdateUserId);
        Assert.Same(picture, account.Picture);
        Assert.Equal(11, account.PictureGetUserId);
        Assert.Equal(11, account.PictureDeleteUserId);
    }

    private sealed class TestCurrentUserService(int userId) : ICurrentUserContext
    {
        public int UserId => userId;
        public bool IsAuthenticated => true;
    }

    private sealed class RecordingUserAccountService : IUserAccountService
    {
        public int? UpdatedUserId { get; private set; }
        public string? UpdatedEmail { get; private set; }
        public DateTime? UpdatedDateOfBirth { get; private set; }
        public int? PasswordUserId { get; private set; }
        public string? CurrentPassword { get; private set; }
        public string? NewPassword { get; private set; }
        public int? PictureUpdateUserId { get; private set; }
        public Stream? Picture { get; private set; }
        public int? PictureGetUserId { get; private set; }
        public int? PictureDeleteUserId { get; private set; }

        public Task<int?> FindUserIdByUsernameAsync(string username, CancellationToken cancellationToken = default) => Task.FromResult<int?>(null);

        public Task<(int? UserId, string? Error)> CreateUserAsync(string username, string email, string password, string? firstName, string? lastName, CancellationToken cancellationToken = default) =>
            Task.FromResult<(int? UserId, string? Error)>((1, null));

        public Task<(bool Success, string? Error)> AssignSingleRoleAsync(int userId, string role, CancellationToken cancellationToken = default) =>
            Task.FromResult((true, (string?)null));

        public Task<(bool Success, string? Error)> UpdateExistingUserAsync(int userId, string email, string? firstName, string? lastName, DateTime? dateOfBirth = null, CancellationToken cancellationToken = default)
        {
            UpdatedUserId = userId;
            UpdatedEmail = email;
            UpdatedDateOfBirth = dateOfBirth;
            return Task.FromResult((true, (string?)null));
        }

        public Task<(bool Success, string? Error)> UpdatePictureAsync(int userId, Stream picture, string fileName, string contentType, CancellationToken cancellationToken = default)
        {
            PictureUpdateUserId = userId;
            Picture = picture;
            return Task.FromResult((true, (string?)null));
        }

        public Task<(Stream? Data, string? ContentType)> GetPictureAsync(int userId, CancellationToken cancellationToken = default)
        {
            PictureGetUserId = userId;
            return Task.FromResult<(Stream? Data, string? ContentType)>((null, null));
        }

        public Task<(bool Success, string? Error)> DeletePictureAsync(int userId, CancellationToken cancellationToken = default)
        {
            PictureDeleteUserId = userId;
            return Task.FromResult((true, (string?)null));
        }

        public Task<(bool Success, string? Error)> ChangePasswordAsync(int userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
        {
            PasswordUserId = userId;
            CurrentPassword = currentPassword;
            NewPassword = newPassword;
            return Task.FromResult((true, (string?)null));
        }

        public Task<(bool Success, string? Error)> SetActiveAsync(int userId, bool isActive, CancellationToken cancellationToken = default) =>
            Task.FromResult((true, (string?)null));

        public Task<(bool Success, string? Error)> DeleteUserAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult((true, (string?)null));
    }
}
