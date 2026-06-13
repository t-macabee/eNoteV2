using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Lectures;
using eNote.Application.Features.Lectures.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Lectures
{
    [Route("api/lectures")]
    public sealed class LectureController(ILectureService service) : CoreController
    {
        [HttpGet]
        public async Task<ActionResult<PagedResult<LectureDto>>> GetAll(int page = 1, int pageSize = 20)
        {
            var result = await service.GetPagedAsync(page, pageSize, CurrentUserId);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<LectureDto>> GetById(int id)
        {
            var dto = await service.GetByIdAsync(id, CurrentUserId);
            return Ok(dto);
        }

        [Authorize(Roles = AppRoles.Instructor)]
        [HttpPost]
        public async Task<ActionResult<LectureDto>> Create([FromBody] LectureCreateRequest request)
        {
            var dto = await service.CreateAsync(CurrentUserId, request);
            return Ok(dto);
        }

        [Authorize(Roles = AppRoles.Student)]
        [HttpPost("{id:int}/rsvp")]
        public async Task<ActionResult> Rsvp(int id, [FromBody] RsvpRequest request)
        {
            var resp = await service.RsvpAsync(id, CurrentUserId, request);
            return Ok(resp);
        }
    }
}
