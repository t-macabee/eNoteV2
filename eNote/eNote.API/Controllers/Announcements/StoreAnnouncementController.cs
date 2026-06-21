using eNote.API.Controllers.Base;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Announcements;
using eNote.Application.Features.Announcements.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Announcements
{
    [Authorize(Roles = AppRoles.StoreEmployee)]
    [Route("api/shop/announcements")]
    public sealed class StoreAnnouncementController(IAnnouncementService announcementService) : CoreController
    {
        [HttpGet]
        public async Task<ActionResult<PagedResult<AnnouncementDto>>> GetForStore(int page = 1, int pageSize = 20)
        {
            var result = await announcementService.GetForStoreAsync(page, pageSize);
            return Ok(result);
        }

        [HttpGet("{announcementId:int}")]
        public async Task<ActionResult<AnnouncementDto>> GetById(int announcementId)
        {
            var result = await announcementService.GetByIdForStoreAsync(announcementId);
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<AnnouncementDto>> Create([FromBody] AnnouncementRequest request)
        {
            var result = await announcementService.CreateForStoreAsync(request);
            return CreatedAtAction(nameof(GetById), new
            {
                announcementId = result.Id
            }, result);
        }

        [HttpPut("{announcementId:int}")]
        public async Task<ActionResult<AnnouncementDto>> Update(int announcementId, [FromBody] AnnouncementRequest request)
        {
            var result = await announcementService.UpdateForStoreAsync(announcementId, request);
            return Ok(result);
        }

        [HttpDelete("{announcementId:int}")]
        public async Task<IActionResult> Delete(int announcementId)
        {
            await announcementService.DeleteForStoreAsync(announcementId);
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
}
