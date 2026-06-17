using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Instruments;
using eNote.Application.Features.Instruments.Search;
using eNote.Application.Features.Instruments.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Instruments
{
    [Authorize(Roles = AppRoles.StoreEmployee)]
    [Route("api/shop/instruments")]
    public sealed class InstrumentController(IInstrumentService instrumentService) : CoreController
    {
        [HttpGet]
        public async Task<ActionResult<PagedResult<InstrumentDto>>> GetPaged([FromQuery] InstrumentSearchObject search)
        {
            PagedResult<InstrumentDto> result = await instrumentService.GetPagedAsync(search);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<InstrumentDto>> GetById(int id)
        {
            InstrumentDto result = await instrumentService.GetByIdAsync(id);
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<InstrumentDto>> Create([FromBody] InstrumentCreateRequest request)
        {
            InstrumentDto result = await instrumentService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new
            {
                id = result.Id
            }, result);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<InstrumentDto>> Update(int id, [FromBody] InstrumentUpdateRequest request)
        {
            InstrumentDto result = await instrumentService.UpdateAsync(id, request);
            return Ok(result);
        }

        [HttpPost("{id:int}/image")]
        [ProducesResponseType(typeof(InstrumentDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<InstrumentDto>> UploadImage(int id, IFormFile file, CancellationToken ct)
        {
            await using Stream stream = file.OpenReadStream();
            InstrumentDto result = await instrumentService.UploadImageAsync(id, stream, file.FileName, file.ContentType, ct);
            return Ok(result);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await instrumentService.DeleteAsync(id);
            return NoContent();
        }
    }
}
