using eNote.Application.Common.Paging;
using eNote.Application.Features.Instruments;
using eNote.Application.Features.Instruments.Search;
using eNote.Application.Features.Instruments.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Instruments
{
    [ApiController]
    [AllowAnonymous]
    [Route("api/instruments/public")]
    public sealed class PublicInstrumentController(IInstrumentService instrumentService) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<PagedResult<InstrumentDto>>> GetPaged([FromQuery] InstrumentSearchObject search)
        {
            var result = await instrumentService.GetPublicPagedAsync(search);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<InstrumentDto>> GetById(int id)
        {
            var result = await instrumentService.GetPublicByIdAsync(id);
            return Ok(result);
        }
    }
}
