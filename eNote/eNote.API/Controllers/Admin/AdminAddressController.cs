using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.ReferenceData.Addresses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Admin;

[Authorize(Roles = AppRoles.Administrator)]
[Route("api/admin/addresses")]
public sealed class AdminAddressController(IAddressService service) : CoreController
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<AddressReferenceDto>>> GetPaged(int page = 1, int pageSize = 20)
    {
        var result = await service.GetPagedAsync(page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AddressReferenceDto>> GetById(int id)
    {
        var dto = await service.GetByIdAsync(id);
        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<AddressReferenceDto>> Create([FromBody] AddressRequest request)
    {
        var dto = await service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AddressReferenceDto>> Update(int id, [FromBody] AddressRequest request)
    {
        var dto = await service.UpdateAsync(id, request);
        return Ok(dto);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
