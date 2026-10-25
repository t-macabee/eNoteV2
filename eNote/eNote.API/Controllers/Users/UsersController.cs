using eNote.API.Controllers.Base;
using eNote.API.Extensions;
using eNote.Application.Common.Files;
using eNote.Application.Constants;
using eNote.Application.Features.Identity.Auth.Services;
using eNote.Application.Features.Identity.Users;
using eNote.Application.Features.Identity.Users.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace eNote.API.Controllers.Users;

[Route("api/v{version:apiVersion}/users")]
public sealed class UsersController(UserProfileService profileService, UserSelfService selfService, IPictureAccessService pictureAccess, IUserAccountService accountService, ITokenRevocationService tokenRevocation) : CoreController
{
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileResponse>> GetCurrentUser(CancellationToken cancellationToken)
    {
        var profile = await profileService.GetCurrentUserAsync(cancellationToken);

        return Ok(profile);
    }

    [HttpPut("me")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        await selfService.UpdateProfileAsync(request, cancellationToken);

        return NoContent();
    }

    [HttpPut("me/picture")]
    [RequestSizeLimit(FileUploadLimits.MaxRequestBytes)]
    [EnableRateLimiting(RateLimitingExtensions.UploadsPolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadPicture(IFormFile? file, CancellationToken cancellationToken)
    {
        ValidateUpload(file);

        await using var stream = file!.OpenReadStream();
        await selfService.UpdatePictureAsync(stream, file.ContentType, cancellationToken);

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
        await selfService.DeletePictureAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("me")]
    [Authorize(Roles = AppRoles.Student)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteProfile([FromBody] DeleteProfileRequest request, CancellationToken cancellationToken)
    {
        await selfService.DeleteProfileAsync(request, cancellationToken);
        await tokenRevocation.RevokeAsync(CurrentTokenJti, CurrentTokenExpiresAtUtc, cancellationToken);

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
        await selfService.ChangePasswordAsync(request, cancellationToken);

        return NoContent();
    }
}
