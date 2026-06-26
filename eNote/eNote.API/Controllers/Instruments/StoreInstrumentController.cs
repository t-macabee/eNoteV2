using eNote.API.Controllers.Base;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Rentals.Instruments;
using eNote.Application.Features.Rentals.Instruments.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Instruments;

[Authorize(Roles = AppRoles.StoreEmployee)]
[Route("api/shop/instruments")]
public sealed class StoreInstrumentController(IInstrumentService instrumentService) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<InstrumentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<InstrumentDto>>> GetPaged([FromQuery] InstrumentSearchObject search)
    {
        var result = await instrumentService.GetPagedAsync(search);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(InstrumentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstrumentDto>> GetById(int id)
    {
        var result = await instrumentService.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(InstrumentDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<InstrumentDto>> Create([FromBody] InstrumentCreateRequest request)
    {
        var result = await instrumentService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new
        {
            id = result.Id
        }, result);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(InstrumentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstrumentDto>> Update(int id, [FromBody] InstrumentUpdateRequest request)
    {
        var result = await instrumentService.UpdateAsync(id, request);
        return Ok(result);
    }

    [HttpPost("{id:int}/image")]
    [ProducesResponseType(typeof(InstrumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InstrumentDto>> UploadImage(int id, IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = Messages.FileNotProvided });
        }

        await using Stream stream = file.OpenReadStream();
        var result = await instrumentService.UploadImageAsync(id, stream, file.FileName, file.ContentType, ct);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id)
    {
        await instrumentService.DeleteAsync(id);
        return NoContent();
    }
}
