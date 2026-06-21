using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Announcements;
using eNote.Application.Features.Announcements.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Announcements
{
    [Authorize(Roles = AppRoles.Student)]
    [Route("api/student/announcements")]
    public sealed class StudentAnnouncementController(IAnnouncementService announcementService) : CoreController
    {
        [HttpGet]
        public async Task<ActionResult<PagedResult<AnnouncementDto>>> GetFeed(int page = 1, int pageSize = 20)
        {
            var result = await announcementService.GetFeedForStudentAsync(page, pageSize);
            return Ok(result);
        }
    }
}
