using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Rentals.ReferenceData.Cities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Admin;

[Authorize(Roles = AppRoles.Administrator)]
[Route("api/v{version:apiVersion}/admin/cities")]
public sealed class AdminCityController(CityService service) : CoreController
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CityDto>>> GetPaged([FromQuery] CitySearchObject search, CancellationToken cancellationToken)
    {
        PagedResult<CityDto> result = await service.GetPagedAsync(search, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<CityDto>> GetById(int id, CancellationToken cancellationToken)
    {
        CityDto dto = await service.GetByIdAsync(id, cancellationToken);
        return Ok(dto);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<CityDto>> Create([FromBody] CityRequest request, CancellationToken cancellationToken)
    {
        CityDto dto = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<CityDto>> Update(int id, [FromBody] CityRequest request, CancellationToken cancellationToken)
    {
        CityDto dto = await service.UpdateAsync(id, request, cancellationToken);
        return Ok(dto);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
