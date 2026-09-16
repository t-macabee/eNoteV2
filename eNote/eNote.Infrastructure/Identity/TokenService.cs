using eNote.Application.Common.Time;
using eNote.Application.Features.Identity.Auth.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace eNote.Infrastructure.Identity;

public sealed class TokenService(IConfiguration configuration, IClock clock) : ITokenService
{
    public const string SecurityStampClaimType = "AspNet.Identity.SecurityStamp";

    private readonly string _jwtKey = configuration["Jwt:Key"]!;
    private readonly string? _jwtIssuer = configuration["Jwt:Issuer"];
    private readonly string? _jwtAudience = configuration["Jwt:Audience"];
    private readonly int _jwtExpirationDays = int.Parse(
        configuration["Jwt:ExpirationDays"]
            ?? throw new InvalidOperationException("Jwt:ExpirationDays is required."));

    public string GenerateToken(int userId, string username, IList<string> roles, bool isManager = false, string? securityStamp = null)
    {
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

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtIssuer,
            audience: _jwtAudience,
            claims: claims,
            expires: clock.UtcNow.AddDays(_jwtExpirationDays),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
