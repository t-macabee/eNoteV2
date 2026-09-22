using eNote.Infrastructure.Identity;
using eNote.Tests.TestUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

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
    public void GenerateToken_IsSignedWithConfiguredKey()
    {
        var key = "test-key-that-is-longer-than-32-characters";
        var service = CreateService(expirationMinutes: 30, key: key);

        var token = service.GenerateToken(1, "jdoe", ["Student"]);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        Assert.NotNull(jwt);
        Assert.Equal("Issuer", jwt.Issuer);
        Assert.Contains(jwt.Audiences, a => a == "Audience");
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
        var options = services.BuildServiceProvider().GetRequiredService<IOptions<JwtOptions>>();

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
