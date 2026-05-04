using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Features.Instruments.DTOs;
using eNote.Application.Features.Instruments.Requests;
using eNote.Application.Features.Instruments.Search;
using eNote.Application.Features.Instruments.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Instruments
{
    [Authorize]
    [Route("api/instruments")]
    public sealed class InstrumentController(IInstrumentService instrumentService) : CoreController
    {
        private readonly IInstrumentService _instrumentService = instrumentService;

        [HttpGet]
        public async Task<ActionResult<PagedResult<InstrumentDto>>> GetAll([FromQuery] InstrumentSearchObject search)
        {
            var result = await _instrumentService.GetPagedAsync(search, CurrentUserId);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<InstrumentDto>> GetById(int id)
        {
            var result = await _instrumentService.GetByIdAsync(id, CurrentUserId);
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<InstrumentDto>> Insert([FromBody] InstrumentCreateRequest request)
        {
            var result = await _instrumentService.InsertAsync(request, CurrentUserId);
            return Ok(result);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<InstrumentDto>> Update(int id, [FromBody] InstrumentUpdateRequest request)
        {
            var result = await _instrumentService.UpdateAsync(id, request, CurrentUserId);
            return Ok(result);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _instrumentService.DeleteAsync(id, CurrentUserId);
            return NoContent();
        }
    }
}
