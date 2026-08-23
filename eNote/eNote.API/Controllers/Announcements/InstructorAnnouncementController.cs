using eNote.API.Controllers.Base;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Communication.Announcements;
using eNote.Application.Features.Communication.Announcements.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Announcements;

[Authorize(Roles = AppRoles.Instructor)]
[Route("api/v{version:apiVersion}/instructor/courses/{courseId:int}/announcements")]
public sealed class InstructorAnnouncementController(InstructorAnnouncementService announcementService) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AnnouncementDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AnnouncementDto>>> GetForCourse(int courseId, [FromQuery] AnnouncementSearchObject search, CancellationToken cancellationToken)
    {
        var result = await announcementService.GetForCourseAsync(courseId, search, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{announcementId:int}")]
    [ProducesResponseType(typeof(AnnouncementDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AnnouncementDto>> GetById(int courseId, int announcementId, CancellationToken cancellationToken)
    {
        var result = await announcementService.GetByIdForCourseAsync(courseId, announcementId, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(AnnouncementDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<AnnouncementDto>> Create(int courseId, [FromBody] AnnouncementRequest request, CancellationToken cancellationToken)
    {
        var result = await announcementService.CreateForCourseAsync(courseId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new
        {
            courseId,
            announcementId = result.Id
        }, result);
    }

    [HttpPut("{announcementId:int}")]
    [ProducesResponseType(typeof(AnnouncementDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AnnouncementDto>> Update(int courseId, int announcementId, [FromBody] AnnouncementRequest request, CancellationToken cancellationToken)
    {
        var result = await announcementService.UpdateForCourseAsync(courseId, announcementId, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{announcementId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int courseId, int announcementId, CancellationToken cancellationToken)
    {
        await announcementService.DeleteForCourseAsync(courseId, announcementId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{announcementId:int}/image")]
    [ProducesResponseType(typeof(AnnouncementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AnnouncementDto>> UploadImage(int courseId, int announcementId, IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = Messages.FileNotProvided });
        }

        await using Stream stream = file.OpenReadStream();

        var result = await announcementService.UploadImageForCourseAsync(courseId, announcementId, stream, file.FileName, file.ContentType, ct);

        return Ok(result);
    }
}
