using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.InstrumentRentals;
using eNote.Application.Features.InstrumentRentals.Search;
using eNote.Application.Features.InstrumentRentals.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.InstrumentRentals
{
    [Authorize(Roles = AppRoles.Student)]
    [Route("api/student/rentals")]
    public sealed class StudentRentalController(IRentalQueryService queryService, IRentalCommandService commandService) : CoreController
    {
        [HttpGet]
        public async Task<ActionResult<PagedResult<InstrumentRentalDto>>> GetPaged([FromQuery] InstrumentRentalSearchObject search)
        {
            var result = await queryService.GetPagedForStudentAsync(CurrentUserId, search);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<InstrumentRentalDto>> GetById(int id)
        {
            var dto = await queryService.GetByIdForStudentAsync(id, CurrentUserId);
            return Ok(dto);
        }

        [HttpPost]
        public async Task<ActionResult<InstrumentRentalDto>> Create([FromBody] RentalCreateRequest request)
        {
            var dto = await commandService.CreateRequestAsync(CurrentUserId, request);
            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
        }

        [HttpPost("{id:int}/cancel")]
        public async Task<ActionResult<InstrumentRentalDto>> Cancel(int id, [FromBody] RentalStatusResponse response)
        {
            var dto = await commandService.CancelAsync(id, CurrentUserId, response);
            return Ok(dto);
        }
    }
}
