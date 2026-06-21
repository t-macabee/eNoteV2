using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.ReferenceData.InstrumentTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Admin;

[Authorize(Roles = AppRoles.Administrator)]
[Route("api/admin/instrument-types")]
public sealed class AdminInstrumentTypeController(IInstrumentTypeService service) : CoreController
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<InstrumentTypeDto>>> GetPaged(int page = 1, int pageSize = 20)
    {
        var result = await service.GetPagedAsync(page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<InstrumentTypeDto>> GetById(int id)
    {
        var dto = await service.GetByIdAsync(id);
        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<InstrumentTypeDto>> Create([FromBody] InstrumentTypeRequest request)
    {
        var dto = await service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<InstrumentTypeDto>> Update(int id, [FromBody] InstrumentTypeRequest request)
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
