using eNote.API.Controllers.Base;
using eNote.Application.Constants;
using eNote.Application.Features.Announcements;
using eNote.Application.Features.Announcements.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Announcements
{
    [Authorize(Roles = AppRoles.Instructor)]
    [Route("api/instructor/courses/{courseId:int}/announcements")]
    public sealed class InstructorAnnouncementController(IAnnouncementService announcementService) : CoreController
    {
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<AnnouncementDto>>> GetForCourse(int courseId)
        {
            var result = await announcementService.GetForCourseAsync(CurrentUserId, courseId);
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<AnnouncementDto>> Create(int courseId, [FromBody] AnnouncementCreateRequest request)
        {
            var result = await announcementService.CreateForCourseAsync(CurrentUserId, courseId, request);
            return Ok(result);
        }

        [HttpPut("{announcementId:int}")]
        public async Task<ActionResult<AnnouncementDto>> Update(int courseId, int announcementId, [FromBody] AnnouncementUpdateRequest request)
        {
            var result = await announcementService.UpdateForCourseAsync(CurrentUserId, courseId, announcementId, request);
            return Ok(result);
        }

        [HttpDelete("{announcementId:int}")]
        public async Task<IActionResult> Delete(int courseId, int announcementId)
        {
            await announcementService.DeleteForCourseAsync(CurrentUserId, courseId, announcementId);
            return NoContent();
        }
    }
}
