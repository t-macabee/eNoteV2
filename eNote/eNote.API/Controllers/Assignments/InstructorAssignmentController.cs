using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Assignments;
using eNote.Application.Features.Assignments.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Assignments;

[Authorize(Roles = AppRoles.Instructor)]
[Route("api/instructor/lectures/{lectureId:int}/assignments")]
public sealed class InstructorAssignmentController(IAssignmentService service) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AssignmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AssignmentDto>>> GetForLecture(int lectureId, [FromQuery] AssignmentSearchObject search)
    {
        var result = await service.GetForLectureAsync(lectureId, search);
        return Ok(result);
    }

    [HttpGet("{assignmentId:int}")]
    [ProducesResponseType(typeof(AssignmentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AssignmentDto>> GetById(int lectureId, int assignmentId)
    {
        var dto = await service.GetByIdForInstructorAsync(lectureId, assignmentId);
        return Ok(dto);
    }

    [HttpPost]
    [ProducesResponseType(typeof(AssignmentDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<AssignmentDto>> Create(int lectureId, [FromBody] AssignmentRequest request)
    {
        var dto = await service.CreateAsync(lectureId, request);
        return CreatedAtAction(nameof(GetById), new
        {
            lectureId,
            assignmentId = dto.Id
        }, dto);
    }

    [HttpPut("{assignmentId:int}")]
    [ProducesResponseType(typeof(AssignmentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AssignmentDto>> Update(int lectureId, int assignmentId, [FromBody] AssignmentRequest request)
    {
        var dto = await service.UpdateAsync(lectureId, assignmentId, request);
        return Ok(dto);
    }

    [HttpDelete("{assignmentId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int lectureId, int assignmentId)
    {
        await service.DeleteAsync(lectureId, assignmentId);
        return NoContent();
    }
}
