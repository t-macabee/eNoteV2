using eNote.API.Controllers.Base;
using eNote.API.Extensions;
using eNote.Application.Common.Files;
using eNote.Application.Features.Identity.Users;
using eNote.Application.Features.Identity.Users.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace eNote.API.Controllers.Users;

[Route("api/v{version:apiVersion}/users")]
public sealed class UsersController(UserProfileService profileService, UserSelfService selfService, IPictureAccessService pictureAccess, IUserAccountService accountService) : CoreController
{
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileResponse>> GetCurrentUser(CancellationToken cancellationToken)
    {
        var profile = await profileService.GetCurrentUserAsync(cancellationToken);

        if (profile is null)
        {
            return NotFound();
        }

        return Ok(profile);
    }

    [HttpPut("me")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        (var success, var error) = await selfService.UpdateProfileAsync(request, cancellationToken);

        if (!success)
        {
            return BadRequest(new
            {
                message = error
            });
        }

        return NoContent();
    }

    [HttpPut("me/picture")]
    [RequestSizeLimit(FileUploadLimits.MaxRequestBytes)]
    [EnableRateLimiting(RateLimitingExtensions.UploadsPolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadPicture(IFormFile? file, CancellationToken cancellationToken)
    {
        if (ValidateUpload(file) is { } uploadError)
        {
            return uploadError;
        }

        await using var stream = file!.OpenReadStream();
        (var success, var error) = await selfService.UpdatePictureAsync(stream, file.FileName, file.ContentType, cancellationToken);

        if (!success)
        {
            return BadRequest(new { message = error });
        }

        return NoContent();
    }

    [HttpGet("me/picture")]
    [Produces("image/jpeg", "image/png", "image/webp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPicture(CancellationToken cancellationToken)
    {
        (var data, var contentType) = await selfService.GetPictureAsync(cancellationToken);

        if (data is null || contentType is null)
        {
            return NotFound();
        }

        return File(data, contentType);
    }

    [HttpDelete("me/picture")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeletePicture(CancellationToken cancellationToken)
    {
        (var success, var error) = await selfService.DeletePictureAsync(cancellationToken);

        if (!success)
        {
            return BadRequest(new { message = error });
        }

        return NoContent();
    }

    [HttpGet("{id:int}/picture")]
    [Produces("image/jpeg", "image/png", "image/webp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserPicture(int id, CancellationToken cancellationToken)
    {
        if (!await pictureAccess.CanViewPictureAsync(id, cancellationToken))
        {
            return NotFound();
        }

        (var data, var contentType) = await accountService.GetPictureAsync(id, cancellationToken);

        if (data is null || contentType is null)
        {
            return NotFound();
        }

        Response.Headers.CacheControl = "private, no-store";
        Response.Headers.Vary = "Authorization";

        return File(data, contentType);
    }

    [HttpPut("me/password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        (var success, var error) = await selfService.ChangePasswordAsync(request, cancellationToken);

        if (!success)
        {
            return BadRequest(new
            {
                message = error
            });
        }

        return NoContent();
    }
}
