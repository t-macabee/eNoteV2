using eNote.Application.Constants;
using eNote.Application.Features.Rentals.ReferenceData.Addresses;
using eNote.Application.Common.Paging;
using eNote.API.Controllers.Base;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Admin;

[Authorize(Roles = AppRoles.Administrator)]
[Route("api/v{version:apiVersion}/admin/addresses")]
public sealed class AdminAddressController(IAddressService service) : CoreController
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AddressReferenceDto>>> GetPaged([FromQuery] AddressSearchObject search, CancellationToken cancellationToken)
    {
        PagedResult<AddressReferenceDto> result = await service.GetPagedAsync(search, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<AddressReferenceDto>> GetById(int id, CancellationToken cancellationToken)
    {
        AddressReferenceDto dto = await service.GetByIdAsync(id, cancellationToken);
        return Ok(dto);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<AddressReferenceDto>> Create([FromBody] AddressRequest request, CancellationToken cancellationToken)
    {
        AddressReferenceDto dto = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<AddressReferenceDto>> Update(int id, [FromBody] AddressRequest request, CancellationToken cancellationToken)
    {
        AddressReferenceDto dto = await service.UpdateAsync(id, request, cancellationToken);
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
