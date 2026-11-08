using eNote.Application.Constants;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace eNote.Tests.TestUtils;

public sealed class TestAuthenticationOptions : AuthenticationSchemeOptions
{
    public string Role { get; set; } = AppRoles.Student;

    public int UserId { get; set; } = 50;
}

public sealed class TestAuthenticationHandler(
    IOptionsMonitor<TestAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<TestAuthenticationOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var current = Options;

        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, current.UserId.ToString()),
                new Claim(ClaimTypes.Role, current.Role)
            ],
            SchemeName);

        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
