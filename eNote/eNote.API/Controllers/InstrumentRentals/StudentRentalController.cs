using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.InstrumentRentals;
using eNote.Application.Features.InstrumentRentals.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.InstrumentRentals;

[Authorize(Roles = AppRoles.Student)]
[Route("api/student/rentals")]
public sealed class StudentRentalController(IRentalQueryService queryService, IRentalCommandService commandService) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<InstrumentRentalDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<InstrumentRentalDto>>> GetPaged([FromQuery] InstrumentRentalSearchObject search)
    {
        var result = await queryService.GetPagedForStudentAsync(search);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(InstrumentRentalDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstrumentRentalDto>> GetById(int id)
    {
        var dto = await queryService.GetByIdForStudentAsync(id);
        return Ok(dto);
    }

    [HttpPost]
    [ProducesResponseType(typeof(InstrumentRentalDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<InstrumentRentalDto>> Create([FromBody] RentalCreateRequest request)
    {
        var dto = await commandService.CreateRequestAsync(request);
        return CreatedAtAction(nameof(GetById), new
        {
            id = dto.Id
        }, dto);
    }

    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(typeof(InstrumentRentalDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstrumentRentalDto>> Cancel(int id, [FromBody] RentalStatusResponse response)
    {
        var dto = await commandService.CancelAsync(id, response);
        return Ok(dto);
    }
}
