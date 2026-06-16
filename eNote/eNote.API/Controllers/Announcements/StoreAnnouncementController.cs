using eNote.API.Controllers.Base;
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
        public async Task<ActionResult<IReadOnlyList<AnnouncementDto>>> GetForStore()
        {
            var result = await announcementService.GetForStoreAsync();
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
            return CreatedAtAction(nameof(GetById), new { announcementId = result.Id }, result);
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
    }
}
