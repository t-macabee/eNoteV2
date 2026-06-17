using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Lectures;
using eNote.Application.Features.Lectures.Search;
using eNote.Application.Features.Lectures.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Lectures
{
    [Authorize(Roles = AppRoles.Student)]
    [Route("api/student/lectures")]
    public sealed class StudentLectureController(ILectureService service) : CoreController
    {
        [HttpGet]
        public async Task<ActionResult<PagedResult<LectureDto>>> GetAvailable([FromQuery] LectureSearchObject search)
        {
            PagedResult<LectureDto> result = await service.GetPagedForStudentAsync(search);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<LectureDto>> GetById(int id)
        {
            LectureDto dto = await service.GetByIdForStudentAsync(id);
            return Ok(dto);
        }

        [HttpPost("{id:int}/rsvp")]
        public async Task<ActionResult<RsvpResponse>> Rsvp(int id, [FromBody] RsvpRequest request)
        {
            RsvpResponse response = await service.RsvpAsync(id, request);
            return Ok(response);
        }
    }
}
