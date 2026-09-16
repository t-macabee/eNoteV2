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
        var harness = await CreateHarnessAsync(isActive: true);

        var context = await ValidateAsync(harness, harness.User.Id.ToString(), harness.User.SecurityStamp);

        Assert.Null(context.Result);
    }

    [Fact]
    public async Task OnTokenValidated_Rejects_WhenUserInactive()
    {
        var harness = await CreateHarnessAsync(isActive: false);

        var context = await ValidateAsync(harness, harness.User.Id.ToString(), harness.User.SecurityStamp);

        Assert.False(context.Result!.Succeeded);
    }

    [Fact]
    public async Task OnTokenValidated_Rejects_WhenSecurityStampIsStale()
    {
        var harness = await CreateHarnessAsync(isActive: true);

        var context = await ValidateAsync(harness, harness.User.Id.ToString(), "stale-stamp");

        Assert.False(context.Result!.Succeeded);
    }

    [Fact]
    public async Task OnTokenValidated_Rejects_WhenStampClaimMissing()
    {
        var harness = await CreateHarnessAsync(isActive: true);

        var context = await ValidateAsync(harness, harness.User.Id.ToString(), stamp: null);

        Assert.False(context.Result!.Succeeded);
    }

    [Fact]
    public async Task OnTokenValidated_Rejects_WhenUserMissing()
    {
        var harness = await CreateHarnessAsync(isActive: true);

        var context = await ValidateAsync(harness, "9999", "stamp");

        Assert.False(context.Result!.Succeeded);
    }

    [Fact]
    public async Task OnTokenValidated_Rejects_WhenTokenRevoked()
    {
        var harness = await CreateHarnessAsync(isActive: true, revoked: true);

        var context = await ValidateAsync(harness, harness.User.Id.ToString(), harness.User.SecurityStamp);

        Assert.False(context.Result!.Succeeded);
    }

    private static async Task<TokenValidatedContext> ValidateAsync(
        Harness harness,
        string subject,
        string? stamp,
        bool revoked = false)
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

    private static async Task<Harness> CreateHarnessAsync(bool isActive, bool revoked = false)
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
        services.AddSingleton<ITokenRevocationService>(new StubRevocationService(revoked));

        var provider = services.BuildServiceProvider();
        var userManager = provider.GetRequiredService<UserManager<AppUser>>();
        var user = new AppUser { UserName = "jdoe", Email = "jdoe@example.com", IsActive = isActive };
        await userManager.CreateAsync(user, "Password1!");

        return new Harness(provider, user);
    }

    private sealed record Harness(IServiceProvider Services, AppUser User);

    private sealed class StubRevocationService(bool revoked) : ITokenRevocationService
    {
        public Task RevokeAsync(string jti, DateTime expiresAt, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken = default) =>
            Task.FromResult(revoked);
    }
}
