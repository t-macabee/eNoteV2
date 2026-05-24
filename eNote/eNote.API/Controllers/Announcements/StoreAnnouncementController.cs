using eNote.API.Controllers.Base;
using eNote.Application.Constants;
using eNote.Application.Features.Announcements.DTOs;
using eNote.Application.Features.Announcements.Requests;
using eNote.Application.Features.Announcements.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Announcements
{
    [Authorize(Roles = AppRoles.StoreEmployee)]
    [Route("api/shop/announcements")]
    public sealed class StoreAnnouncementController(IAnnouncementService announcementService) : CoreController
    {
        private readonly IAnnouncementService _announcementService = announcementService;

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<AnnouncementDto>>> GetForStore()
        {
            var result = await _announcementService.GetForStoreAsync(CurrentUserId);
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<AnnouncementDto>> Create([FromBody] AnnouncementCreateRequest request)
        {
            var result = await _announcementService.CreateForStoreAsync(CurrentUserId, request);
            return Ok(result);
        }
    }
}
