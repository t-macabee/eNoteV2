using eNote.API.Controllers.Base;
using eNote.Application.Constants;
using eNote.Application.Features.Announcements;
using eNote.Application.Features.Announcements.Services.Interfaces;
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
            var result = await announcementService.GetForStoreAsync(CurrentUserId);
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<AnnouncementDto>> Create([FromBody] AnnouncementCreateRequest request)
        {
            var result = await announcementService.CreateForStoreAsync(CurrentUserId, request);
            return Ok(result);
        }
    }
}
