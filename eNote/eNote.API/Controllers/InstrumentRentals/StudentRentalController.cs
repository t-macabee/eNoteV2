using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Rentals.InstrumentRentals;
using eNote.Application.Features.Rentals.InstrumentRentals.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.InstrumentRentals;

[Authorize(Roles = AppRoles.Student)]
[Route("api/v{version:apiVersion}/student/rentals")]
public sealed class StudentRentalController(RentalQueryService queryService, RentalCommandService commandService) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<InstrumentRentalDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<InstrumentRentalDto>>> GetPaged([FromQuery] InstrumentRentalSearchObject search, CancellationToken cancellationToken)
    {
        var result = await queryService.GetPagedForStudentAsync(search, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(InstrumentRentalDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstrumentRentalDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var dto = await queryService.GetByIdForStudentAsync(id, cancellationToken);
        return Ok(dto);
    }

    [HttpPost]
    [ProducesResponseType(typeof(InstrumentRentalDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<InstrumentRentalDto>> Create([FromBody] RentalCreateRequest request, CancellationToken cancellationToken)
    {
        var dto = await commandService.CreateRequestAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new
        {
            id = dto.Id
        }, dto);
    }

    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(typeof(InstrumentRentalDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstrumentRentalDto>> Cancel(int id, [FromBody] RentalStatusResponse response, CancellationToken cancellationToken)
    {
        var dto = await commandService.CancelAsync(id, response, cancellationToken);
        return Ok(dto);
    }
}
