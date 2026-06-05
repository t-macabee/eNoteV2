using eNote.API.Controllers.Base;
using eNote.Application.Features.Announcements;
using eNote.Application.Features.Announcements.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Announcements
{
    [Route("api/student/announcements")]
    public sealed class StudentAnnouncementController(IAnnouncementService announcementService) : CoreController
    {
        private readonly IAnnouncementService _announcementService = announcementService;

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<AnnouncementDto>>> GetFeed()
        {
            var result = await _announcementService.GetFeedForStudentAsync(CurrentUserId);
            return Ok(result);
        }
    }
}
