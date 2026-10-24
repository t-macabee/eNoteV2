using eNote.API.Controllers.Users;
using eNote.Application.Common.Localization;
using eNote.Application.Features.Identity.Auth.Services;
using eNote.Application.Features.Identity.Users;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Tests.TestUtils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace eNote.Tests.Identity;

public sealed class UsersControllerTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task UploadPicture_ReturnsBadRequest_WhenFilePartMissing()
    {
        var controller = new UsersController(null!, null!, null!, null!, null!);

        var result = await controller.UploadPicture(null, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains(Messages.FileNotProvided, badRequest.Value!.ToString());
    }

    [Fact]
    public async Task DeleteProfile_ReturnsNoContent_AndRevokesTheCurrentToken()
    {
        var revocation = new RecordingTokenRevocationService();
        var controller = CreateController(CreateSelfService(passwordIsValid: true), revocation);

        var result = await controller.DeleteProfile(
            new DeleteProfileRequest { CurrentPassword = "Password1!" },
            CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal("jti-1", revocation.RevokedJti);
    }

    [Fact]
    public async Task DeleteProfile_Throws_WhenCurrentPasswordIsWrong()
    {
        var revocation = new RecordingTokenRevocationService();
        var controller = CreateController(CreateSelfService(passwordIsValid: false), revocation);

        await Assert.ThrowsAsync<CurrentPasswordInvalidException>(() => controller.DeleteProfile(
            new DeleteProfileRequest { CurrentPassword = "Wrong1!" },
            CancellationToken.None));

        Assert.Null(revocation.RevokedJti);
    }

    private static UsersController CreateController(UserSelfService selfService, RecordingTokenRevocationService revocation) =>
        new(null!, selfService, null!, null!, revocation)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = BuildPrincipal(userId: 5) }
            }
        };

    private static UserSelfService CreateSelfService(bool passwordIsValid)
    {
        var context = TestDbContextFactory.CreateContext(Now);
        var student = new Student(5, Now);
        context.Set<Student>().Add(student);
        context.SaveChanges();

        return new UserSelfService(
            new PasswordCheckingAccountService(passwordIsValid),
            new StubCurrentActor(userId: 5),
            context,
            new FixedClock(Now),
            new FakePaymentGateway(),
            new StubFileStorageService(),
            NullLogger<UserSelfService>.Instance);
    }

    private static ClaimsPrincipal BuildPrincipal(int userId)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, "jti-1"),
            new(JwtRegisteredClaimNames.Exp, new DateTimeOffset(Now.AddHours(1)).ToUnixTimeSeconds().ToString())
        };

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
    }

    private sealed class RecordingTokenRevocationService : ITokenRevocationService
    {
        public string? RevokedJti { get; private set; }

        public Task RevokeAsync(string jti, DateTime expiresAt, CancellationToken cancellationToken = default)
        {
            RevokedJti = jti;
            return Task.CompletedTask;
        }

        public Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken = default) =>
            Task.FromResult(RevokedJti == jti);
    }

    private sealed class PasswordCheckingAccountService(bool passwordIsValid) : IUserAccountService
    {
        public Task<int?> FindUserIdByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<(int? UserId, string? Error)> CreateUserAsync(string username, string email, string password, string? firstName, string? lastName, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<(bool Success, string? Error)> AssignSingleRoleAsync(int userId, string role, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<(bool Success, string? Error)> UpdateExistingUserAsync(int userId, string email, string? firstName, string? lastName, DateTime? dateOfBirth = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<(bool Success, string? Error)> UpdatePictureAsync(int userId, Stream picture, string fileName, string contentType, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<(Stream? Data, string? ContentType)> GetPictureAsync(int userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<(bool Success, string? Error)> DeletePictureAsync(int userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<(bool Success, string? Error)> ChangePasswordAsync(int userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<bool> VerifyPasswordAsync(int userId, string currentPassword, CancellationToken cancellationToken = default) =>
            Task.FromResult(passwordIsValid);
        public Task<(bool Success, string? Error)> SetActiveAsync(int userId, bool isActive, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<(bool Success, string? Error, string? PicturePath)> CloseAccountAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult((true, (string?)null, (string?)null));
        public Task<(bool Success, string? Error, string? PicturePath)> DeleteUserAsync(int userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
