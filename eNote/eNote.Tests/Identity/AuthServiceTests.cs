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

        await auth.ResetPasswordAsync(new ResetPasswordRequest
        {
            Email = "jdoe@example.com",
            Token = token,
            NewPassword = "Newpassword1!"
        });

        Assert.True(await harness.UserManager.CheckPasswordAsync(harness.User, "Newpassword1!"));
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

    private static async Task<Harness> CreateHarnessAsync(string withRole)
    {
        var context = TestDbContextFactory.CreateContext(Now);
        var identity = IdentityTestHarness.Create(context);

        await identity.RoleManager.CreateAsync(new AppRole { Name = withRole });
        var user = new AppUser { UserName = "jdoe", Email = "jdoe@example.com", IsActive = true };
        await identity.UserManager.CreateAsync(user, "Password1!");
        await identity.UserManager.AddToRoleAsync(user, withRole);

        return new Harness(context, identity.UserManager, identity.RoleManager, identity.SignInManager, user);
    }

    private sealed class Harness(
        ENoteContext context,
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager,
        SignInManager<AppUser> signInManager,
        AppUser user)
    {
        public ENoteContext Context => context;
        public UserManager<AppUser> UserManager => userManager;
        public RoleManager<AppRole> RoleManager => roleManager;
        public AppUser User => user;
        public RecordingEmailService Email { get; } = new();
        public List<(string Jti, DateTime ExpiresAt)> Revocations { get; } = [];

        public AuthService CreateAuthService(IUserProvisioningService? provisioning = null) =>
            new(UserManager,
                signInManager,
                new StubTokenService(),
                provisioning ?? new StubUserProvisioningService(),
                new RecordingTokenRevocationService(Revocations),
                Email,
                new StubHostEnvironment(),
                NullLogger<AuthService>.Instance);
    }

    private sealed class StubTokenService : ITokenService
    {
        public string GenerateToken(int userId, string username, IList<string> roles) => "generated-token";
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

        public Task<(RegistrationResult? Registration, string? Error)> RegisterStudentAsync(RegisterRequest request, CancellationToken cancellationToken = default)
        {
            LastRegisterRequest = request;
            return Task.FromResult<(RegistrationResult?, string?)>((new RegistrationResult(7, request.Username.Trim(), ["Student"]), null));
        }

        public Task<(int UserId, string? Error)> ProvisionUserAsync(UserProvisionRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult((7, (string?)null));

        public Task UpdateMembershipAsync(int userId, UpdateMembershipRequest request, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Test";
        public string ApplicationName { get; set; } = "eNote.Tests";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
