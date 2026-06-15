using eNote.API.Controllers.Base;
using eNote.Application.Constants;
using eNote.Application.Features.Announcements;
using eNote.Application.Features.Announcements.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Announcements
{
    [Authorize(Roles = AppRoles.Student)]
    [Route("api/student/announcements")]
    public sealed class StudentAnnouncementController(IAnnouncementService announcementService) : CoreController
    {
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<AnnouncementDto>>> GetFeed()
        {
            var result = await announcementService.GetFeedForStudentAsync();
            return Ok(result);
        }
    }
}
