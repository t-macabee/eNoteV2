using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Communication.Announcements;
using eNote.Application.Features.Communication.Announcements.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Announcements;

[Authorize(Roles = AppRoles.Student)]
[Route("api/student/announcements")]
public sealed class StudentAnnouncementController(IStudentAnnouncementService announcementService) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AnnouncementDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AnnouncementDto>>> GetFeed([FromQuery] AnnouncementSearchObject search)
    {
        var result = await announcementService.GetFeedForStudentAsync(search);
        return Ok(result);
    }
}
