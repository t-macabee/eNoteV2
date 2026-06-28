using eNote.API.Controllers.Base;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Communication.Announcements;
using eNote.Application.Features.Communication.Announcements.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Announcements;

[Authorize(Roles = AppRoles.StoreEmployee)]
[Route("api/shop/announcements")]
public sealed class StoreAnnouncementController(IStoreAnnouncementService announcementService) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AnnouncementDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AnnouncementDto>>> GetForStore([FromQuery] AnnouncementSearchObject search, CancellationToken cancellationToken)
    {
        var result = await announcementService.GetForStoreAsync(search, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{announcementId:int}")]
    [ProducesResponseType(typeof(AnnouncementDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AnnouncementDto>> GetById(int announcementId, CancellationToken cancellationToken)
    {
        var result = await announcementService.GetByIdForStoreAsync(announcementId, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(AnnouncementDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<AnnouncementDto>> Create([FromBody] AnnouncementRequest request, CancellationToken cancellationToken)
    {
        var result = await announcementService.CreateForStoreAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new
        {
            announcementId = result.Id
        }, result);
    }

    [HttpPut("{announcementId:int}")]
    [ProducesResponseType(typeof(AnnouncementDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AnnouncementDto>> Update(int announcementId, [FromBody] AnnouncementRequest request, CancellationToken cancellationToken)
    {
        var result = await announcementService.UpdateForStoreAsync(announcementId, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{announcementId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int announcementId, CancellationToken cancellationToken)
    {
        await announcementService.DeleteForStoreAsync(announcementId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{announcementId:int}/image")]
    [ProducesResponseType(typeof(AnnouncementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AnnouncementDto>> UploadImage(int announcementId, IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = Messages.FileNotProvided });
        }

        await using Stream stream = file.OpenReadStream();

        var result = await announcementService.UploadImageForStoreAsync(announcementId, stream, file.FileName, file.ContentType, ct);

        return Ok(result);
    }
}
