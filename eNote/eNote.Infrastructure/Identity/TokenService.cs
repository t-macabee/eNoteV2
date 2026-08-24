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
    private readonly string _jwtKey = configuration["Jwt:Key"]!;
    private readonly string? _jwtIssuer = configuration["Jwt:Issuer"];
    private readonly string? _jwtAudience = configuration["Jwt:Audience"];
    private readonly int _jwtExpirationDays = configuration.GetValue("Jwt:ExpirationDays", 7);

    public string GenerateToken(int userId, string username, IList<string> roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

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
