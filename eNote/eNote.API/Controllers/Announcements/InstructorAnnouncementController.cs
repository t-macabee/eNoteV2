using eNote.API.Controllers.Base;
using eNote.Application.Common.Localization;
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
            var result = await announcementService.GetForCourseAsync(courseId, page, pageSize);
            return Ok(result);
        }

        [HttpGet("{announcementId:int}")]
        public async Task<ActionResult<AnnouncementDto>> GetById(int courseId, int announcementId)
        {
            var result = await announcementService.GetByIdForCourseAsync(courseId, announcementId);
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<AnnouncementDto>> Create(int courseId, [FromBody] AnnouncementRequest request)
        {
            var result = await announcementService.CreateForCourseAsync(courseId, request);
            return CreatedAtAction(nameof(GetById), new
            {
                courseId,
                announcementId = result.Id
            }, result);
        }

        [HttpPut("{announcementId:int}")]
        public async Task<ActionResult<AnnouncementDto>> Update(int courseId, int announcementId, [FromBody] AnnouncementRequest request)
        {
            var result = await announcementService.UpdateForCourseAsync(courseId, announcementId, request);
            return Ok(result);
        }

        [HttpDelete("{announcementId:int}")]
        public async Task<IActionResult> Delete(int courseId, int announcementId)
        {
            await announcementService.DeleteForCourseAsync(courseId, announcementId);
            return NoContent();
        }

        [HttpPost("{announcementId:int}/image")]
        [ProducesResponseType(typeof(AnnouncementDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<AnnouncementDto>> UploadImage(int courseId, int announcementId, IFormFile? file, CancellationToken ct)
        {
            if (file is null || file.Length == 0)
            {
                return BadRequest(new { message = Messages.FileNotProvided });
            }

            await using Stream stream = file.OpenReadStream();

            var result = await announcementService.UploadImageForCourseAsync(courseId, announcementId, stream, file.FileName, file.ContentType, ct);

            return Ok(result);
        }
    }
}
