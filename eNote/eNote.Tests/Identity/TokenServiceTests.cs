using eNote.Infrastructure.Identity;
using eNote.Tests.TestUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace eNote.Tests.Identity;

public sealed class TokenServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void GenerateToken_IncludesSubjectUsernameAndRoles()
    {
        var service = CreateService(expirationMinutes: 30);

        var token = service.GenerateToken(42, "jdoe", ["Student", "Instructor"]);

        var claims = ReadClaims(token);
        Assert.Equal("42", claims.Single(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal("jdoe", claims.Single(c => c.Type == JwtRegisteredClaimNames.UniqueName).Value);
        Assert.Equal(2, claims.Count(c => c.Type == ClaimTypes.Role));
        Assert.Contains(claims, c => c.Type == ClaimTypes.Role && c.Value == "Student");
        Assert.Contains(claims, c => c.Type == ClaimTypes.Role && c.Value == "Instructor");
    }

    [Fact]
    public void GenerateToken_ExpiresAfterConfiguredMinutes()
    {
        var service = CreateService(expirationMinutes: 30);

        var token = service.GenerateToken(1, "jdoe", ["Student"]);

        var claims = ReadClaims(token);
        var expiresAt = claims.Single(c => c.Type == JwtRegisteredClaimNames.Exp).Value;
        var expected = new DateTimeOffset(Now.AddMinutes(30)).ToUnixTimeSeconds();
        Assert.Equal(expected, long.Parse(expiresAt));
    }

    [Fact]
    public async Task GenerateToken_IsSignedWithConfiguredKey()
    {
        var key = "test-key-that-is-longer-than-32-characters";
        var service = CreateService(expirationMinutes: 30, key: key);

        var token = service.GenerateToken(1, "jdoe", ["Student"]);

        var parameters = new TokenValidationParameters
        {
            ValidIssuer = "Issuer",
            ValidAudience = "Audience",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidateLifetime = false
        };
        var result = await new JwtSecurityTokenHandler().ValidateTokenAsync(token, parameters);
        Assert.True(result.IsValid, result.Exception?.Message);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GenerateToken_AddsManagerClaim_OnlyForManagers(bool isManager)
    {
        var service = CreateService(expirationMinutes: 30);

        var token = service.GenerateToken(1, "jdoe", ["StoreEmployee"], isManager);

        var managerClaims = ReadClaims(token).Where(c => c.Type == "is_manager").ToList();
        if (isManager)
        {
            Assert.Equal("true", Assert.Single(managerClaims).Value);
        }
        else
        {
            Assert.Empty(managerClaims);
        }
    }

    [Fact]
    public void GenerateToken_AddsSecurityStampClaim_WhenGiven()
    {
        var service = CreateService(expirationMinutes: 30);

        var token = service.GenerateToken(1, "jdoe", ["Student"], securityStamp: "stamp-1");

        Assert.Equal("stamp-1", ReadClaims(token).Single(c => c.Type == TokenService.SecurityStampClaimType).Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void GenerateToken_OmitsSecurityStampClaim_WhenBlank(string? securityStamp)
    {
        var service = CreateService(expirationMinutes: 30);

        var token = service.GenerateToken(1, "jdoe", ["Student"], securityStamp: securityStamp);

        Assert.DoesNotContain(ReadClaims(token), c => c.Type == TokenService.SecurityStampClaimType);
    }

    [Fact]
    public void GenerateToken_GivesEachTokenItsOwnJti()
    {
        var service = CreateService(expirationMinutes: 30);

        var first = ReadClaims(service.GenerateToken(1, "jdoe", ["Student"])).Single(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        var second = ReadClaims(service.GenerateToken(1, "jdoe", ["Student"])).Single(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void GenerateToken_Throws_WhenJwtKeyMissing()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "Issuer",
            ["Jwt:Audience"] = "Audience",
            ["Jwt:ExpirationMinutes"] = "30"
        }).Build();

        var services = new ServiceCollection();
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations();
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<JwtOptions>>();

        var service = new TokenService(options, new FixedClock(Now));

        var ex = Assert.Throws<OptionsValidationException>(() =>
            service.GenerateToken(1, "jdoe", ["Student"]));

        Assert.Contains("Key", ex.Message);
    }

    private static TokenService CreateService(int expirationMinutes, string? key = null) =>
        new(Options.Create(BuildOptions(expirationMinutes, key)), new FixedClock(Now));

    private static JwtOptions BuildOptions(int expirationMinutes, string? key) => new()
    {
        Key = key ?? "test-signing-key-that-is-32-characters-long!!",
        Issuer = "Issuer",
        Audience = "Audience",
        ExpirationMinutes = expirationMinutes
    };

    private static List<Claim> ReadClaims(string token) =>
        new JwtSecurityTokenHandler().ReadJwtToken(token).Claims.ToList();
}
