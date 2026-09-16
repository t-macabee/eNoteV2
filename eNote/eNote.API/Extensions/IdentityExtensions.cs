using eNote.Application.Common.Localization;
using eNote.Application.Features.Identity.Auth.Services;
using eNote.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace eNote.API.Extensions;

public static class IdentityExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration config)
    {
        var key = Encoding.UTF8.GetBytes(config["Jwt:Key"]!);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = config["Jwt:Issuer"],
                    ValidAudience = config["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    },
                    OnTokenValidated = OnTokenValidated
                };
            });

        return services;
    }

    public static async Task OnTokenValidated(TokenValidatedContext context)
    {
        var jti = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Jti);

        var revocation = context.HttpContext.RequestServices.GetRequiredService<ITokenRevocationService>();

        if (!string.IsNullOrWhiteSpace(jti) && await revocation.IsRevokedAsync(jti, context.HttpContext.RequestAborted))
        {
            context.Fail(Messages.TokenRevoked);
            return;
        }

        if (!int.TryParse(context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId))
        {
            context.Fail(Messages.TokenInvalid);
            return;
        }

        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<AppUser>>();
        AppUser? user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null || !user.IsActive)
        {
            context.Fail(Messages.TokenInvalid);
            return;
        }

        var stamp = context.Principal!.FindFirstValue(TokenService.SecurityStampClaimType);

        if (string.IsNullOrWhiteSpace(stamp) || stamp != user.SecurityStamp)
        {
            context.Fail(Messages.TokenInvalid);
        }
    }
}
