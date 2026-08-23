using eNote.Application.Common.Paging;
using eNote.Application.Features.Rentals.Instruments;
using eNote.Application.Features.Rentals.Instruments.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Instruments;

[ApiController]
[AllowAnonymous]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/instruments/public")]
public sealed class PublicInstrumentController(InstrumentService instrumentService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<InstrumentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<InstrumentDto>>> GetPaged([FromQuery] InstrumentSearchObject search, CancellationToken cancellationToken)
    {
        var result = await instrumentService.GetPublicPagedAsync(search, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(InstrumentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstrumentDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await instrumentService.GetPublicByIdAsync(id, cancellationToken);
        return Ok(result);
    }
}
