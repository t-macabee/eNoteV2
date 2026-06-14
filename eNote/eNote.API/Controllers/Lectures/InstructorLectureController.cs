using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Lectures;
using eNote.Application.Features.Lectures.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Lectures
{
    [Authorize(Roles = AppRoles.Instructor)]
    [Route("api/instructor/lectures")]
    public sealed class InstructorLectureController(ILectureService service) : CoreController
    {
        [HttpGet]
        public async Task<ActionResult<PagedResult<LectureDto>>> GetMyLectures(int page = 1, int pageSize = 20)
        {
            var result = await service.GetPagedForInstructorAsync(CurrentUserId, page, pageSize);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<LectureDto>> GetById(int id)
        {
            var dto = await service.GetByIdForInstructorAsync(id, CurrentUserId);
            return Ok(dto);
        }

        [HttpPost]
        public async Task<ActionResult<LectureDto>> Create([FromBody] LectureCreateRequest request)
        {
            var dto = await service.CreateAsync(CurrentUserId, request);
            return Ok(dto);
        }
    }
}
