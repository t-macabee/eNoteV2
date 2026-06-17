using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Announcements;
using eNote.Application.Features.Announcements.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Announcements
{
    [Authorize(Roles = AppRoles.Instructor)]
    [Route("api/instructor/courses/{courseId:int}/announcements")]
    public sealed class InstructorAnnouncementController(IAnnouncementService announcementService) : CoreController
    {
        [HttpGet]
        public async Task<ActionResult<PagedResult<AnnouncementDto>>> GetForCourse(int courseId, int page = 1, int pageSize = 20)
        {
            PagedResult<AnnouncementDto> result = await announcementService.GetForCourseAsync(courseId, page, pageSize);
            return Ok(result);
        }

        [HttpGet("{announcementId:int}")]
        public async Task<ActionResult<AnnouncementDto>> GetById(int courseId, int announcementId)
        {
            AnnouncementDto result = await announcementService.GetByIdForCourseAsync(courseId, announcementId);
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<AnnouncementDto>> Create(int courseId, [FromBody] AnnouncementRequest request)
        {
            AnnouncementDto result = await announcementService.CreateForCourseAsync(courseId, request);
            return CreatedAtAction(nameof(GetById), new
            {
                courseId,
                announcementId = result.Id
            }, result);
        }

        [HttpPut("{announcementId:int}")]
        public async Task<ActionResult<AnnouncementDto>> Update(int courseId, int announcementId, [FromBody] AnnouncementRequest request)
        {
            AnnouncementDto result = await announcementService.UpdateForCourseAsync(courseId, announcementId, request);
            return Ok(result);
        }

        [HttpDelete("{announcementId:int}")]
        public async Task<IActionResult> Delete(int courseId, int announcementId)
        {
            await announcementService.DeleteForCourseAsync(courseId, announcementId);
            return NoContent();
        }
    }
}
