using eNote.API.Controllers.Base;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Communication.Announcements;
using eNote.Application.Features.Communication.Announcements.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Announcements;

[Route("api/v{version:apiVersion}/announcements")]
public sealed class AnnouncementController(
    InstructorAnnouncementService instructorAnnouncementService,
    StoreAnnouncementService storeAnnouncementService,
    StudentAnnouncementFeedService feedService) : CoreController
{
    // ── Instructor actions ──────────────────────────────────────────

    [Authorize(Roles = AppRoles.Instructor)]
    [HttpGet("~/api/v{version:apiVersion}/instructor/courses/{courseId:int}/announcements")]
    [ProducesResponseType(typeof(PagedResult<AnnouncementDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AnnouncementDto>>> GetForCourse(int courseId, [FromQuery] AnnouncementSearchObject search, CancellationToken cancellationToken)
    {
        var result = await instructorAnnouncementService.GetForCourseAsync(courseId, search, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = AppRoles.Instructor)]
    [HttpGet("~/api/v{version:apiVersion}/instructor/courses/{courseId:int}/announcements/{announcementId:int}")]
    [ProducesResponseType(typeof(AnnouncementDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AnnouncementDto>> GetByIdForCourse(int courseId, int announcementId, CancellationToken cancellationToken)
    {
        var result = await instructorAnnouncementService.GetByIdForCourseAsync(courseId, announcementId, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = AppRoles.Instructor)]
    [HttpPost("~/api/v{version:apiVersion}/instructor/courses/{courseId:int}/announcements")]
    [ProducesResponseType(typeof(AnnouncementDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<AnnouncementDto>> CreateForCourse(int courseId, [FromBody] AnnouncementRequest request, CancellationToken cancellationToken)
    {
        var result = await instructorAnnouncementService.CreateForCourseAsync(courseId, request, cancellationToken);
        return CreatedAtAction(nameof(GetByIdForCourse), new { courseId, announcementId = result.Id }, result);
    }

    [Authorize(Roles = AppRoles.Instructor)]
    [HttpPut("~/api/v{version:apiVersion}/instructor/courses/{courseId:int}/announcements/{announcementId:int}")]
    [ProducesResponseType(typeof(AnnouncementDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AnnouncementDto>> UpdateForCourse(int courseId, int announcementId, [FromBody] AnnouncementRequest request, CancellationToken cancellationToken)
    {
        var result = await instructorAnnouncementService.UpdateForCourseAsync(courseId, announcementId, request, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = AppRoles.Instructor)]
    [HttpDelete("~/api/v{version:apiVersion}/instructor/courses/{courseId:int}/announcements/{announcementId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteForCourse(int courseId, int announcementId, CancellationToken cancellationToken)
    {
        await instructorAnnouncementService.DeleteForCourseAsync(courseId, announcementId, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = AppRoles.Instructor)]
    [HttpPost("~/api/v{version:apiVersion}/instructor/courses/{courseId:int}/announcements/{announcementId:int}/image")]
    [ProducesResponseType(typeof(AnnouncementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AnnouncementDto>> UploadImageForCourse(int courseId, int announcementId, IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = Messages.FileNotProvided });
        }

        await using Stream stream = file.OpenReadStream();
        var result = await instructorAnnouncementService.UploadImageForCourseAsync(courseId, announcementId, stream, file.FileName, file.ContentType, ct);
        return Ok(result);
    }

    // ── Store employee actions ──────────────────────────────────────

    [Authorize(Roles = AppRoles.StoreEmployee)]
    [HttpGet("~/api/v{version:apiVersion}/shop/announcements")]
    [ProducesResponseType(typeof(PagedResult<AnnouncementDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AnnouncementDto>>> GetForStore([FromQuery] AnnouncementSearchObject search, CancellationToken cancellationToken)
    {
        var result = await storeAnnouncementService.GetForStoreAsync(search, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = AppRoles.StoreEmployee)]
    [HttpGet("~/api/v{version:apiVersion}/shop/announcements/{announcementId:int}")]
    [ProducesResponseType(typeof(AnnouncementDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AnnouncementDto>> GetByIdForStore(int announcementId, CancellationToken cancellationToken)
    {
        var result = await storeAnnouncementService.GetByIdForStoreAsync(announcementId, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = AppRoles.StoreEmployee)]
    [HttpPost("~/api/v{version:apiVersion}/shop/announcements")]
    [ProducesResponseType(typeof(AnnouncementDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<AnnouncementDto>> CreateForStore([FromBody] AnnouncementRequest request, CancellationToken cancellationToken)
    {
        var result = await storeAnnouncementService.CreateForStoreAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetByIdForStore), new { announcementId = result.Id }, result);
    }

    [Authorize(Roles = AppRoles.StoreEmployee)]
    [HttpPut("~/api/v{version:apiVersion}/shop/announcements/{announcementId:int}")]
    [ProducesResponseType(typeof(AnnouncementDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AnnouncementDto>> UpdateForStore(int announcementId, [FromBody] AnnouncementRequest request, CancellationToken cancellationToken)
    {
        var result = await storeAnnouncementService.UpdateForStoreAsync(announcementId, request, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = AppRoles.StoreEmployee)]
    [HttpDelete("~/api/v{version:apiVersion}/shop/announcements/{announcementId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteForStore(int announcementId, CancellationToken cancellationToken)
    {
        await storeAnnouncementService.DeleteForStoreAsync(announcementId, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = AppRoles.StoreEmployee)]
    [HttpPost("~/api/v{version:apiVersion}/shop/announcements/{announcementId:int}/image")]
    [ProducesResponseType(typeof(AnnouncementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AnnouncementDto>> UploadImageForStore(int announcementId, IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = Messages.FileNotProvided });
        }

        await using Stream stream = file.OpenReadStream();
        var result = await storeAnnouncementService.UploadImageForStoreAsync(announcementId, stream, file.FileName, file.ContentType, ct);
        return Ok(result);
    }

    // ── Student actions ─────────────────────────────────────────────

    [Authorize(Roles = AppRoles.Student)]
    [HttpGet("~/api/v{version:apiVersion}/student/announcements")]
    [ProducesResponseType(typeof(PagedResult<AnnouncementDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AnnouncementDto>>> GetFeed([FromQuery] AnnouncementSearchObject search, CancellationToken cancellationToken)
    {
        var result = await feedService.GetFeedForStudentAsync(search, cancellationToken);
        return Ok(result);
    }
}
