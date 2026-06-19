using eNote.Application.Common.Localization;
using eNote.Application.Features.Auth.Services;
using eNote.Infrastructure.Data;
using eNote.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace eNote.API.Extensions
{
    public static class IdentityExtensions
    {
        public static IServiceCollection AddApplicationIdentity(this IServiceCollection services)
        {
            services.AddIdentityCore<AppUser>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequiredLength = 8;
                options.Lockout.AllowedForNewUsers = false;
            })
            .AddRoles<AppRole>()
            .AddEntityFrameworkStores<ENoteContext>()
            .AddSignInManager<SignInManager<AppUser>>()
            .AddDefaultTokenProviders();

            return services;
        }

        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration config)
        {
            byte[] key = Encoding.UTF8.GetBytes(config["Jwt:Key"]!);

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
                        OnTokenValidated = async context =>
                        {
                            string? jti = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Jti);

                            if (string.IsNullOrWhiteSpace(jti))
                            {
                                return;
                            }

                            ITokenRevocationService revocation = context.HttpContext.RequestServices.GetRequiredService<ITokenRevocationService>();

                            if (await revocation.IsRevokedAsync(jti, context.HttpContext.RequestAborted))
                            {
                                context.Fail(Messages.TokenRevoked);
                            }
                        }
                    };
                });

            return services;
        }
    }
}
