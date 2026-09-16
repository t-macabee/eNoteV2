using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Features.Identity.Auth;
using eNote.Application.Features.Identity.Auth.Services;
using eNote.Application.Features.Identity.Users;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Infrastructure.Identity;
using eNote.Tests.TestUtils;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

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
    }

    [Fact]
    public async Task LoginAsync_Throws_WhenUserInactive()
    {
        var harness = await CreateHarnessAsync(withRole: "Student");
        harness.User.IsActive = false;
        await harness.UserManager.UpdateAsync(harness.User);
        var auth = harness.CreateAuthService();

        await Assert.ThrowsAsync<AuthenticationException>(() =>
            auth.LoginAsync(new LoginRequest { Username = "jdoe", Password = "Password1!" }));
    }

    [Fact]
    public async Task LoginAsync_Throws_WhenPasswordInvalid()
    {
        var harness = await CreateHarnessAsync(withRole: "Student");
        var auth = harness.CreateAuthService();

        await Assert.ThrowsAsync<AuthenticationException>(() =>
            auth.LoginAsync(new LoginRequest { Username = "jdoe", Password = "wrong-password" }));
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

        await Assert.ThrowsAsync<BusinessException>(() =>
            auth.LoginAsync(new LoginRequest { Username = "jdoe", Password = "Password1!" }));
    }

    [Fact]
    public async Task RegisterAsync_ReturnsTokenFromProvisioning()
    {
        var harness = await CreateHarnessAsync(withRole: "Student");
        var provisioning = new StubUserProvisioningService();
        var auth = harness.CreateAuthService(provisioning: provisioning);
        var request = new RegisterRequest { Username = "newstudent", Email = "new@example.com", Password = "Password1!" };

        var response = await auth.RegisterAsync(request);

        Assert.Equal(7, response.UserId);
        Assert.Equal("newstudent", response.Username);
        Assert.Equal("generated-token", response.Token);
        Assert.Same(request, provisioning.LastRegisterRequest);
    }

    [Fact]
    public async Task ForgotPasswordAsync_SendsEmail_ForExistingUser()
    {
        var harness = await CreateHarnessAsync(withRole: "Student");
        var auth = harness.CreateAuthService();

        var response = await auth.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "jdoe@example.com" });

        Assert.Equal(Messages.PasswordResetEmailSent, response.Message);
        Assert.Single(harness.Email.PasswordResets);
        Assert.Equal("jdoe@example.com", harness.Email.PasswordResets[0].Email);
        Assert.NotEmpty(harness.Email.PasswordResets[0].Token);
    }

    [Fact]
    public async Task ForgotPasswordAsync_DoesNotSend_ForUnknownUser()
    {
        var harness = await CreateHarnessAsync(withRole: "Student");
        var auth = harness.CreateAuthService();

        var response = await auth.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "nobody@example.com" });

        Assert.Equal(Messages.PasswordResetEmailSent, response.Message);
        Assert.Empty(harness.Email.PasswordResets);
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
            Email = "jdoe@example.com",
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

        await Assert.ThrowsAsync<BusinessException>(() =>
            auth.ResetPasswordAsync(new ResetPasswordRequest
            {
                Email = "nobody@example.com",
                Token = "token",
                NewPassword = "newpassword1"
            }));
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

    [Fact]
    public async Task ForgotPasswordAsync_ReturnsSuccess_WhenEmailSendFails()
    {
        var harness = await CreateHarnessAsync(withRole: "Student");
        var auth = harness.CreateAuthService(email: new ThrowingEmailService());

        var response = await auth.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "jdoe@example.com" });

        Assert.Equal(Messages.PasswordResetEmailSent, response.Message);
    }

    private static async Task<Harness> CreateHarnessAsync(string withRole)
    {
        var context = TestDbContextFactory.CreateContext(Now);
        var identity = IdentityTestHarness.Create(context);

        await identity.RoleManager.CreateAsync(new AppRole { Name = withRole });
        var user = new AppUser { UserName = "jdoe", Email = "jdoe@example.com", IsActive = true };
        await identity.UserManager.CreateAsync(user, "Password1!");
        await identity.UserManager.AddToRoleAsync(user, withRole);

        return new Harness(identity.UserManager, identity.RoleManager, identity.SignInManager, user);
    }

    private sealed class Harness(
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager,
        SignInManager<AppUser> signInManager,
        AppUser user)
    {
        public UserManager<AppUser> UserManager => userManager;
        public RoleManager<AppRole> RoleManager => roleManager;
        public AppUser User => user;
        public RecordingEmailService Email { get; } = new();
        public List<(string Jti, DateTime ExpiresAt)> Revocations { get; } = [];

        public AuthService CreateAuthService(IUserProvisioningService? provisioning = null, IEmailService? email = null) =>
            new(UserManager,
                signInManager,
                new StubTokenService(),
                provisioning ?? new StubUserProvisioningService(),
                new RecordingTokenRevocationService(Revocations),
                email ?? Email,
                new StubHostEnvironment(),
                NullLogger<AuthService>.Instance);
    }

    private sealed class ThrowingEmailService : IEmailService
    {
        public Task SendPasswordResetAsync(string email, string token, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("SMTP unavailable");
    }

    private sealed class StubTokenService : ITokenService
    {
        public string GenerateToken(int userId, string username, IList<string> roles, bool isManager = false, string? securityStamp = null) => "generated-token";
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

        public Task<(RegistrationResult? Registration, string? Error)> RegisterStudentAsync(RegisterRequest request, CancellationToken cancellationToken = default)
        {
            LastRegisterRequest = request;
            return Task.FromResult<(RegistrationResult?, string?)>(RegisterError is null
                ? (new RegistrationResult(7, request.Username.Trim(), ["Student"]), null)
                : (null, RegisterError));
        }

        public Task<(int UserId, string? Error)> ProvisionUserAsync(UserProvisionRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult((7, (string?)null));

        public Task UpdateMembershipAsync(int userId, UpdateMembershipRequest request, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<(bool Success, string? Error)> DeactivateUserAsync(int userId, CancellationToken cancellationToken = default) => Task.FromResult((true, (string?)null));
        public Task<(bool Success, string? Error)> SetUserActiveAsync(int userId, bool isActive, CancellationToken cancellationToken = default) => Task.FromResult((true, (string?)null));
        public Task<(bool Success, string? Error)> DeleteUserAsync(int userId, CancellationToken cancellationToken = default) => Task.FromResult((true, (string?)null));
        public Task<bool> IsStoreManagerAsync(int userId, CancellationToken cancellationToken = default) => Task.FromResult(false);

        public Task<(int UserId, string? Error)> ProvisionStudentByInstructorAsync(DelegatedUserCreateRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult((7, (string?)null));

        public Task<(int UserId, string? Error)> ProvisionEmployeeByManagerAsync(DelegatedUserCreateRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult((7, (string?)null));
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Test";
        public string ApplicationName { get; set; } = "eNote.Tests";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
