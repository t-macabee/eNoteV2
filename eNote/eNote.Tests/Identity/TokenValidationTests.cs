using eNote.API.Extensions;
using eNote.Application.Features.Identity.Auth.Services;
using eNote.Infrastructure.Identity;
using eNote.Tests.TestUtils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace eNote.Tests.Identity;

public sealed class TokenValidationTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task OnTokenValidated_Accepts_WhenUserActiveAndStampMatches()
    {
        await using var harness = await CreateHarnessAsync(isActive: true);

        var context = await ValidateAsync(harness, harness.User.Id.ToString(), harness.User.SecurityStamp);

        Assert.Null(context.Result);
    }

    [Fact]
    public async Task OnTokenValidated_Rejects_WhenUserInactive()
    {
        await using var harness = await CreateHarnessAsync(isActive: false);

        var context = await ValidateAsync(harness, harness.User.Id.ToString(), harness.User.SecurityStamp);

        Assert.False(context.Result!.Succeeded);
    }

    [Fact]
    public async Task OnTokenValidated_Rejects_WhenSecurityStampIsStale()
    {
        await using var harness = await CreateHarnessAsync(isActive: true);

        var context = await ValidateAsync(harness, harness.User.Id.ToString(), "stale-stamp");

        Assert.False(context.Result!.Succeeded);
    }

    [Fact]
    public async Task OnTokenValidated_Rejects_WhenStampClaimMissing()
    {
        await using var harness = await CreateHarnessAsync(isActive: true);

        var context = await ValidateAsync(harness, harness.User.Id.ToString(), stamp: null);

        Assert.False(context.Result!.Succeeded);
    }

    [Fact]
    public async Task OnTokenValidated_Rejects_WhenUserMissing()
    {
        await using var harness = await CreateHarnessAsync(isActive: true);

        var context = await ValidateAsync(harness, "9999", "stamp");

        Assert.False(context.Result!.Succeeded);
    }

    [Fact]
    public async Task OnTokenValidated_Rejects_WhenTokenJtiIsRevoked()
    {
        await using var harness = await CreateHarnessAsync(isActive: true, revokedJtis: ["jti-1"]);

        var context = await ValidateAsync(harness, harness.User.Id.ToString(), harness.User.SecurityStamp);

        Assert.False(context.Result!.Succeeded);
    }

    [Fact]
    public async Task OnTokenValidated_Accepts_WhenOtherJtiIsRevoked()
    {
        await using var harness = await CreateHarnessAsync(isActive: true, revokedJtis: ["jti-2"]);

        var context = await ValidateAsync(harness, harness.User.Id.ToString(), harness.User.SecurityStamp);

        Assert.Null(context.Result);
    }

    [Fact]
    public async Task OnTokenValidated_Rejects_AfterAccountIsClosed()
    {
        await using var harness = await CreateHarnessAsync(isActive: true);
        var stampBefore = harness.User.SecurityStamp;
        var accountService = new UserAccountService(
            harness.Services.GetRequiredService<UserManager<AppUser>>(),
            new RecordingFileStorageService());

        await accountService.CloseAccountAsync(harness.User.Id);

        var context = await ValidateAsync(harness, harness.User.Id.ToString(), stampBefore);

        Assert.False(context.Result!.Succeeded);
    }

    private static async Task<TokenValidatedContext> ValidateAsync(
        Harness harness,
        string subject,
        string? stamp)
    {
        var httpContext = new DefaultHttpContext { RequestServices = harness.Services };
        var context = new TokenValidatedContext(
            httpContext,
            new AuthenticationScheme("Bearer", "Bearer", typeof(JwtBearerHandler)),
            new JwtBearerOptions())
        {
            Principal = BuildPrincipal(subject, stamp)
        };

        await IdentityExtensions.OnTokenValidated(context);

        return context;
    }

    private static ClaimsPrincipal BuildPrincipal(string subject, string? stamp)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, subject),
            new(JwtRegisteredClaimNames.Jti, "jti-1")
        };

        if (stamp is not null)
        {
            claims.Add(new Claim(TokenService.SecurityStampClaimType, stamp));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
    }

    private static async Task<Harness> CreateHarnessAsync(bool isActive, params string[] revokedJtis)
    {
        var context = TestDbContextFactory.CreateContext(Now);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        services.AddDataProtection();
        services.AddScoped(_ => context);
        services.AddIdentityCore<AppUser>()
            .AddEntityFrameworkStores<ENoteContext>()
            .AddDefaultTokenProviders();
        services.AddSingleton<ITokenRevocationService>(new StubRevocationService(revokedJtis));

        var provider = services.BuildServiceProvider();
        var userManager = provider.GetRequiredService<UserManager<AppUser>>();
        var user = new AppUser { UserName = "jdoe", Email = "jdoe@example.com", IsActive = isActive };
        await userManager.CreateAsync(user, "Password1!");

        return new Harness(provider, user);
    }

    private sealed record Harness(IServiceProvider Services, AppUser User) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => Services is IAsyncDisposable disposable
            ? disposable.DisposeAsync()
            : ValueTask.CompletedTask;
    }

    private sealed class StubRevocationService(params string[] revokedJtis) : ITokenRevocationService
    {
        private readonly HashSet<string> _revokedJtis = [.. revokedJtis];

        public Task RevokeAsync(string jti, DateTime expiresAt, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken = default) =>
            Task.FromResult(_revokedJtis.Contains(jti));
    }
}
