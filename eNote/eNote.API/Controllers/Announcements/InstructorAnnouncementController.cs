using eNote.API.Controllers.Base;
using eNote.Application.Features.Announcements;
using eNote.Application.Features.Announcements.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Announcements
{
    [Route("api/instructor/courses/{courseId:int}/announcements")]
    public sealed class InstructorAnnouncementController(IAnnouncementService announcementService) : CoreController
    {
        private readonly IAnnouncementService _announcementService = announcementService;

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<AnnouncementDto>>> GetForCourse(int courseId)
        {
            var result = await _announcementService.GetForCourseAsync(CurrentUserId, courseId);
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<AnnouncementDto>> Create(int courseId, [FromBody] AnnouncementCreateRequest request)
        {
            var result = await _announcementService.CreateForCourseAsync(CurrentUserId, courseId, request);
            return Ok(result);
        }
    }
}
