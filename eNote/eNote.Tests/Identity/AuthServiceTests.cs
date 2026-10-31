using eNote.Application.Common.Localization;
using eNote.Application.Features.Identity.Auth;
using eNote.Application.Features.Identity.Auth.Services;
using eNote.Application.Features.Identity.Users;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Contracts.Identity;
using eNote.Domain.Entities.Communication;
using eNote.Infrastructure.Identity;
using eNote.Tests.TestUtils;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;

namespace eNote.Tests.Identity;

public sealed class AuthServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task LoginAsync_ReturnsToken_WhenCredentialsValid()
    {
        var harness = await CreateHarnessAsync(withRole: "Student");
        var auth = harness.CreateAuthService();

        var response = await auth.LoginAsync(new LoginRequest { Username = "jdoe", Password = "Password1!" });

        Assert.Equal(harness.User.Id, response.UserId);
        Assert.Equal("jdoe", response.Username);
        Assert.Equal(["Student"], response.Roles);
        Assert.Equal("generated-token", response.Token);

        var stamp = await harness.UserManager.GetSecurityStampAsync(harness.User);
        var issued = harness.Tokens.Single();
        Assert.Equal(harness.User.Id, issued.UserId);
        Assert.Equal("jdoe", issued.Username);
        Assert.Equal(["Student"], issued.Roles);
        Assert.False(issued.IsManager);
        Assert.Equal(stamp, issued.SecurityStamp);
    }

    [Fact]
    public async Task LoginAsync_Throws_WhenUserInactive()
    {
        var harness = await CreateHarnessAsync(withRole: "Student");
        harness.User.IsActive = false;
        await harness.UserManager.UpdateAsync(harness.User);
        var auth = harness.CreateAuthService();

        var exception = await Assert.ThrowsAsync<AuthenticationException>(() =>
            auth.LoginAsync(new LoginRequest { Username = "jdoe", Password = "Password1!" }));

        Assert.Equal(Messages.InvalidCredentials, exception.Message);
    }

    [Fact]
    public async Task LoginAsync_Throws_WhenPasswordInvalid()
    {
        var harness = await CreateHarnessAsync(withRole: "Student");
        var auth = harness.CreateAuthService();

        var exception = await Assert.ThrowsAsync<AuthenticationException>(() =>
            auth.LoginAsync(new LoginRequest { Username = "jdoe", Password = "wrong-password" }));

        Assert.Equal(Messages.InvalidCredentials, exception.Message);
    }

    [Fact]
    public async Task LoginAsync_GivesTheSameError_ForUnknownUserAndWrongPassword()
    {
        var harness = await CreateHarnessAsync(withRole: "Student");
        var auth = harness.CreateAuthService();

        var unknown = await Assert.ThrowsAsync<AuthenticationException>(() =>
            auth.LoginAsync(new LoginRequest { Username = "nobody", Password = "Password1!" }));

        Assert.Equal(Messages.InvalidCredentials, unknown.Message);
    }

    [Fact]
    public async Task LoginAsync_LocksTheAccount_AfterFiveWrongPasswords()
    {
        var harness = await CreateHarnessAsync(withRole: "Student");
        var auth = harness.CreateAuthService();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await Assert.ThrowsAsync<AuthenticationException>(() =>
                auth.LoginAsync(new LoginRequest { Username = "jdoe", Password = "wrong-password" }));
        }

        var exception = await Assert.ThrowsAsync<AuthenticationException>(() =>
            auth.LoginAsync(new LoginRequest { Username = "jdoe", Password = "Password1!" }));

        Assert.Equal(Messages.AccountLocked, exception.Message);
    }

    [Fact]
    public async Task LoginAsync_DoesNotLockTheAccount_AfterFourWrongPasswords()
    {
        var harness = await CreateHarnessAsync(withRole: "Student");
        var auth = harness.CreateAuthService();

        for (var attempt = 0; attempt < 4; attempt++)
        {
            await Assert.ThrowsAsync<AuthenticationException>(() =>
                auth.LoginAsync(new LoginRequest { Username = "jdoe", Password = "wrong-password" }));
        }

        var response = await auth.LoginAsync(new LoginRequest { Username = "jdoe", Password = "Password1!" });

        Assert.Equal("generated-token", response.Token);
    }

    [Fact]
    public async Task LoginAsync_TrimsTheUsername()
    {
        var harness = await CreateHarnessAsync(withRole: "Student");
        var auth = harness.CreateAuthService();

        var response = await auth.LoginAsync(new LoginRequest { Username = "  jdoe  ", Password = "Password1!" });

        Assert.Equal("jdoe", response.Username);
        Assert.Equal("jdoe", harness.Tokens.Single().Username);
    }

    [Fact]
    public async Task LoginAsync_PutsTheSecurityStampInTheToken()
    {
        var harness = await CreateHarnessAsync(withRole: "Student");
        var auth = harness.CreateAuthService();

        await auth.LoginAsync(new LoginRequest { Username = "jdoe", Password = "Password1!" });

        var stamp = await harness.UserManager.GetSecurityStampAsync(harness.User);
        Assert.False(string.IsNullOrEmpty(stamp));
        Assert.Equal(stamp, harness.Tokens.Single().SecurityStamp);
    }

    [Fact]
    public async Task LoginAsync_MarksAStoreManager_OnlyForStoreEmployees()
    {
        var harness = await CreateHarnessAsync(withRole: "StoreEmployee");
        var provisioning = new StubUserProvisioningService { ManagerUserIds = [harness.User.Id] };
        var auth = harness.CreateAuthService(provisioning: provisioning);

        await auth.LoginAsync(new LoginRequest { Username = "jdoe", Password = "Password1!" });

        Assert.True(harness.Tokens.Single().IsManager);
    }

    [Fact]
    public async Task LoginAsync_DoesNotMarkAManager_ForOtherRoles()
    {
        var harness = await CreateHarnessAsync(withRole: "Student");
        var provisioning = new StubUserProvisioningService { ManagerUserIds = [harness.User.Id] };
        var auth = harness.CreateAuthService(provisioning: provisioning);

        await auth.LoginAsync(new LoginRequest { Username = "jdoe", Password = "Password1!" });

        Assert.False(harness.Tokens.Single().IsManager);
    }

    [Fact]
    public async Task LoginAsync_DoesNotMarkAManager_ForAStoreEmployeeWhoIsNotOne()
    {
        var harness = await CreateHarnessAsync(withRole: "StoreEmployee");
        var auth = harness.CreateAuthService();

        await auth.LoginAsync(new LoginRequest { Username = "jdoe", Password = "Password1!" });

        Assert.False(harness.Tokens.Single().IsManager);
    }

    [Fact]
    public async Task ForgotPasswordAsync_DoesNotEnqueue_ForInactiveUser_AndAnswersTheSame()
    {
        var harness = await CreateHarnessAsync(withRole: "Student");
        harness.User.IsActive = false;
        await harness.UserManager.UpdateAsync(harness.User);
        var auth = harness.CreateAuthService();

        var response = await auth.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "jdoe@example.com" });

        Assert.Equal(Messages.PasswordResetEmailSent, response.Message);
        Assert.Empty(harness.Context.Set<NotificationOutbox>());
    }

    [Fact]
    public async Task ResetPasswordAsync_Throws_AndKeepsThePassword_WhenTokenInvalid()
    {
        var harness = await CreateHarnessAsync(withRole: "Student");
        var auth = harness.CreateAuthService();

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            auth.ResetPasswordAsync(new ResetPasswordRequest
            {
                Email = "jdoe@example.com",
                Token = "not-a-real-token",
                NewPassword = "Newpassword1!"
            }));

        Assert.Equal(Messages.PasswordResetFailed, exception.Message);
        Assert.True(await harness.UserManager.CheckPasswordAsync(harness.User, "Password1!"));
    }

    [Fact]
    public async Task ResetPasswordAsync_Throws_AndKeepsThePassword_ForInactiveUser()
    {
        var harness = await CreateHarnessAsync(withRole: "Student");
        var token = await harness.UserManager.GeneratePasswordResetTokenAsync(harness.User);
        harness.User.IsActive = false;
        await harness.UserManager.UpdateAsync(harness.User);
        var auth = harness.CreateAuthService();

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            auth.ResetPasswordAsync(new ResetPasswordRequest
            {
                Email = "jdoe@example.com",
                Token = token,
                NewPassword = "Newpassword1!"
            }));

        Assert.Equal(Messages.PasswordResetFailed, exception.Message);
        Assert.True(await harness.UserManager.CheckPasswordAsync(harness.User, "Password1!"));
    }

    [Fact]
    public async Task ResetPasswordAsync_Throws_WhenNewPasswordIsTooWeak()
    {
        var harness = await CreateHarnessAsync(withRole: "Student");
        var auth = harness.CreateAuthService();
        var token = await harness.UserManager.GeneratePasswordResetTokenAsync(harness.User);

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            auth.ResetPasswordAsync(new ResetPasswordRequest
            {
                Email = "jdoe@example.com",
                Token = token,
                NewPassword = "a"
            }));

        Assert.Equal(Messages.PasswordResetFailed, exception.Message);
        Assert.True(await harness.UserManager.CheckPasswordAsync(harness.User, "Password1!"));
    }

    [Fact]
    public async Task LoginAsync_Throws_WhenUserLockedOut()
    {
        var harness = await CreateHarnessAsync(withRole: "Student");
        harness.User.LockoutEnd = DateTimeOffset.UtcNow.AddHours(1);
        await harness.UserManager.UpdateAsync(harness.User);
        var auth = harness.CreateAuthService();

        await Assert.ThrowsAsync<AuthenticationException>(() =>
            auth.LoginAsync(new LoginRequest { Username = "jdoe", Password = "Password1!" }));
    }

    [Fact]
    public async Task LoginAsync_Throws_WhenUserHasMultipleRoles()
    {
        var harness = await CreateHarnessAsync(withRole: "Student");
        await harness.RoleManager.CreateAsync(new AppRole { Name = "Instructor" });
        await harness.UserManager.AddToRoleAsync(harness.User, "Instructor");
        var auth = harness.CreateAuthService();

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            auth.LoginAsync(new LoginRequest { Username = "jdoe", Password = "Password1!" }));

        Assert.Equal(Messages.UserSingleRoleRequired, exception.Message);
    }

    [Fact]
    public async Task LoginAsync_Throws_WhenUserHasNoRole()
    {
        var harness = await CreateHarnessAsync(withRole: null);
        var auth = harness.CreateAuthService();

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            auth.LoginAsync(new LoginRequest { Username = "jdoe", Password = "Password1!" }));

        Assert.Equal(Messages.UserSingleRoleRequired, exception.Message);
    }

    [Fact]
    public async Task RegisterAsync_ReturnsTokenFromProvisioning()
    {
        var harness = await CreateHarnessAsync(withRole: "Student");
        var provisioning = new StubUserProvisioningService { UserId = harness.User.Id };
        var auth = harness.CreateAuthService(provisioning: provisioning);
        var request = new RegisterRequest { Username = "newstudent", Email = "new@example.com", Password = "Password1!" };

        var response = await auth.RegisterAsync(request);

        Assert.Equal(harness.User.Id, response.UserId);
        Assert.Equal("newstudent", response.Username);
        Assert.Equal("generated-token", response.Token);
        Assert.Same(request, provisioning.LastRegisterRequest);

        var stamp = await harness.UserManager.GetSecurityStampAsync(harness.User);
        Assert.Equal(stamp, harness.Tokens.Single().SecurityStamp);
    }

    [Fact]
    public async Task ForgotPasswordAsync_EnqueuesOneRequest_ForExistingUser()
    {
        var harness = await CreateHarnessAsync(withRole: "Student");
        var auth = harness.CreateAuthService();

        var response = await auth.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "  jdoe@example.com  " });

        Assert.Equal(Messages.PasswordResetEmailSent, response.Message);
        Assert.NotEqual(1, harness.User.Id);

        var outbox = Assert.Single(harness.Context.Set<NotificationOutbox>());
        Assert.Equal(NotificationMessageTypes.PasswordResetRequested, outbox.MessageType);

        var payload = JsonSerializer.Deserialize<PasswordResetRequested>(outbox.PayloadJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(payload);
        Assert.Equal(harness.User.Id, payload.UserId);
    }

    [Fact]
    public async Task ForgotPasswordAsync_DoesNotEnqueue_ForUnknownUser()
    {
        var harness = await CreateHarnessAsync(withRole: "Student");
        var auth = harness.CreateAuthService();

        var response = await auth.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "nobody@example.com" });

        Assert.Equal(Messages.PasswordResetEmailSent, response.Message);
        Assert.Empty(harness.Context.Set<NotificationOutbox>());
    }

    [Fact]
    public async Task ResetPasswordAsync_ResetsPassword_WithValidToken()
    {
        var harness = await CreateHarnessAsync(withRole: "Student");
        var auth = harness.CreateAuthService();
        var token = await harness.UserManager.GeneratePasswordResetTokenAsync(harness.User);
        var stampBefore = harness.User.SecurityStamp;

        await auth.ResetPasswordAsync(new ResetPasswordRequest
        {
            Email = "  jdoe@example.com  ",
            Token = token,
            NewPassword = "Newpassword1!"
        });

        Assert.True(await harness.UserManager.CheckPasswordAsync(harness.User, "Newpassword1!"));
        Assert.NotEqual(stampBefore, harness.User.SecurityStamp);
    }

    [Fact]
    public async Task ResetPasswordAsync_Throws_ForUnknownUser()
    {
        var harness = await CreateHarnessAsync(withRole: "Student");
        var auth = harness.CreateAuthService();

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            auth.ResetPasswordAsync(new ResetPasswordRequest
            {
                Email = "nobody@example.com",
                Token = "token",
                NewPassword = "newpassword1"
            }));

        Assert.Equal(Messages.PasswordResetFailed, exception.Message);
    }

    [Fact]
    public async Task LogoutAsync_RevokesToken()
    {
        var harness = await CreateHarnessAsync(withRole: "Student");
        var auth = harness.CreateAuthService();
        var expiresAt = Now.AddDays(7);

        await auth.LogoutAsync("jti-1", expiresAt);

        Assert.Equal(("jti-1", expiresAt), harness.Revocations.Single());
    }

    [Fact]
    public async Task RegisterAsync_ThrowsConflictWithEmailMessage_WhenEmailTaken()
    {
        var harness = await CreateHarnessAsync(withRole: "Student");
        var provisioning = new StubUserProvisioningService { RegisterError = Messages.EmailTaken };
        var auth = harness.CreateAuthService(provisioning: provisioning);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            auth.RegisterAsync(new RegisterRequest { Username = "newstudent", Email = "taken@example.com", Password = "Password1!" }));

        Assert.Equal(Messages.EmailTaken, exception.Message);
        Assert.NotEqual(Messages.UsernameTaken, exception.Message);
    }

    private static async Task<Harness> CreateHarnessAsync(string? withRole)
    {
        var context = TestDbContextFactory.CreateContext(Now);
        var identity = IdentityTestHarness.Create(context);

        await identity.UserManager.CreateAsync(
            new AppUser { UserName = "seed", Email = "seed@example.com", IsActive = true },
            "Password1!");

        var user = new AppUser { UserName = "jdoe", Email = "jdoe@example.com", IsActive = true };
        await identity.UserManager.CreateAsync(user, "Password1!");

        if (withRole is not null)
        {
            await identity.RoleManager.CreateAsync(new AppRole { Name = withRole });
            await identity.UserManager.AddToRoleAsync(user, withRole);
        }

        return new Harness(identity.UserManager, identity.RoleManager, identity.SignInManager, context, user);
    }

    private sealed class Harness(
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager,
        SignInManager<AppUser> signInManager,
        ENoteContext context,
        AppUser user)
    {
        public UserManager<AppUser> UserManager => userManager;
        public RoleManager<AppRole> RoleManager => roleManager;
        public ENoteContext Context => context;
        public AppUser User => user;
        public List<(string Jti, DateTime ExpiresAt)> Revocations { get; } = [];
        public List<IssuedToken> Tokens { get; } = [];

        public AuthService CreateAuthService(IUserProvisioningService? provisioning = null) =>
            new(UserManager,
                signInManager,
                new StubTokenService(Tokens),
                provisioning ?? new StubUserProvisioningService(),
                new RecordingTokenRevocationService(Revocations),
                Context,
                new FixedClock(Now),
                NullLogger<AuthService>.Instance);
    }

    private sealed record IssuedToken(int UserId, string Username, IList<string> Roles, bool IsManager, string? SecurityStamp);

    private sealed class StubTokenService(List<IssuedToken>? issued = null) : ITokenService
    {
        public string GenerateToken(int userId, string username, IList<string> roles, bool isManager = false, string? securityStamp = null)
        {
            issued?.Add(new IssuedToken(userId, username, roles, isManager, securityStamp));
            return "generated-token";
        }
    }

    private sealed class RecordingTokenRevocationService(List<(string Jti, DateTime ExpiresAt)> calls) : ITokenRevocationService
    {
        public Task RevokeAsync(string jti, DateTime expiresAt, CancellationToken cancellationToken = default)
        {
            calls.Add((jti, expiresAt));
            return Task.CompletedTask;
        }

        public Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken = default) => Task.FromResult(false);
    }

    private sealed class StubUserProvisioningService : IUserProvisioningService
    {
        public RegisterRequest? LastRegisterRequest { get; private set; }
        public string? RegisterError { get; init; }
        public int UserId { get; init; } = 7;
        public int[] ManagerUserIds { get; init; } = [];

        public Task<RegistrationResult> RegisterStudentAsync(RegisterRequest request, CancellationToken cancellationToken = default)
        {
            LastRegisterRequest = request;

            if (RegisterError is not null)
            {
                throw new ConflictException(RegisterError);
            }

            return Task.FromResult(new RegistrationResult(UserId, request.Username.Trim(), ["Student"]));
        }

        public Task<int> ProvisionUserAsync(UserProvisionRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(7);

        public Task SetUserActiveAsync(int userId, bool isActive, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteUserAsync(int userId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> IsStoreManagerAsync(int userId, CancellationToken cancellationToken = default) => Task.FromResult(ManagerUserIds.Contains(userId));

        public Task<int> ProvisionStudentByInstructorAsync(DelegatedUserCreateRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(7);

        public Task<int> ProvisionEmployeeByManagerAsync(DelegatedUserCreateRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(7);
    }
}
