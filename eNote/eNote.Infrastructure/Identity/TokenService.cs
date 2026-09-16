using eNote.Application.Common.Time;
using eNote.Application.Features.Identity.Auth.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace eNote.Infrastructure.Identity;

public sealed class TokenService(IOptions<JwtOptions> jwtOptions, IClock clock) : ITokenService
{
    public const string SecurityStampClaimType = "AspNet.Identity.SecurityStamp";

    public string GenerateToken(int userId, string username, IList<string> roles, bool isManager = false, string? securityStamp = null)
    {
        var options = jwtOptions.Value;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (isManager)
        {
            claims.Add(new Claim("is_manager", "true"));
        }

        if (!string.IsNullOrWhiteSpace(securityStamp))
        {
            claims.Add(new Claim(SecurityStampClaimType, securityStamp));
        }

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            expires: clock.UtcNow.AddDays(options.ExpirationDays),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
