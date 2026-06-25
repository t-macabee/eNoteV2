using eNote.API.Controllers.Base;
using eNote.Application.Features.Users;
using eNote.Application.Features.Users.Services;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Users;

[Route("api/users")]
public sealed class UsersController(
    IUserProfileService profileService,
    IUserSelfService selfService) : CoreController
{
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileResponse>> GetCurrentUser()
    {
        var profile = await profileService.GetCurrentUserAsync();

        if (profile is null)
        {
            return NotFound();
        }

        return Ok(profile);
    }

    [HttpPut("me")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        (var success, var error) = await selfService.UpdateProfileAsync(request);

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
    [RequestSizeLimit(5 * 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadPicture(IFormFile file)
    {
        if (file.Length == 0)
        {
            return BadRequest(new { message = "No file uploaded." });
        }

        await using var stream = file.OpenReadStream();
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);

        (var success, var error) = await selfService.UpdatePictureAsync(buffer.ToArray());

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
    public async Task<IActionResult> GetPicture()
    {
        (var data, var contentType) = await selfService.GetPictureAsync();

        if (data is null || contentType is null)
        {
            return NotFound();
        }

        return File(data, contentType);
    }

    [HttpDelete("me/picture")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeletePicture()
    {
        (var success, var error) = await selfService.DeletePictureAsync();

        if (!success)
        {
            return BadRequest(new { message = error });
        }

        return NoContent();
    }

    [HttpPut("me/password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        (var success, var error) = await selfService.ChangePasswordAsync(request);

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
