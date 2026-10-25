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

    protected static void ValidateUpload(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            throw new BusinessException(Messages.FileNotProvided);
        }
    }

    protected async Task<ActionResult<TDto>> UploadAsync<TDto>(
        IFormFile? file,
        Func<Stream, string, string, CancellationToken, Task<TDto>> upload,
        CancellationToken cancellationToken)
    {
        ValidateUpload(file);

        await using Stream stream = file!.OpenReadStream();
        var result = await upload(stream, file.FileName, file.ContentType, cancellationToken);
        return new OkObjectResult(result);
    }
}
