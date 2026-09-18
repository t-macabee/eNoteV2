using Asp.Versioning;
using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace eNote.API.Controllers.Base;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
public abstract class CoreController : ControllerBase
{
    protected string CurrentTokenJti => User.FindFirstValue(JwtRegisteredClaimNames.Jti) ?? throw new AuthenticationException(Messages.InvalidUserClaim);

    protected DateTime CurrentTokenExpiresAtUtc
    {
        get
        {
            var exp = User.FindFirstValue(JwtRegisteredClaimNames.Exp);

            if (exp is null || !long.TryParse(exp, out var unixSeconds))
            {
                throw new AuthenticationException(Messages.InvalidUserClaim);
            }

            return DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime;
        }
    }

    protected static IActionResult MapError(string? error, int defaultStatus = StatusCodes.Status400BadRequest)
    {
        if (ErrorMapping.IsNotFound(error))
        {
            return new NotFoundObjectResult(new { message = error });
        }

        if (ErrorMapping.IsConflict(error))
        {
            return new ConflictObjectResult(new { message = error });
        }

        return defaultStatus == StatusCodes.Status404NotFound
            ? new NotFoundObjectResult(new { message = error })
            : new BadRequestObjectResult(new { message = error });
    }
}
