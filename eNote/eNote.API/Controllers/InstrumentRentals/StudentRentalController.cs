using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Features.InstrumentRentals.DTOs;
using eNote.Application.Features.InstrumentRentals.Requests;
using eNote.Application.Features.InstrumentRentals.Search;
using eNote.Application.Features.InstrumentRentals.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.InstrumentRentals
{
    [Authorize]
    [Route("api/student/rentals")]
    public class StudentRentalController(IRentalQueryService queryService, IRentalCommandService commandService) : CoreController
    {
        private readonly IRentalQueryService _queryService = queryService;
        private readonly IRentalCommandService _commandService = commandService;

        [HttpGet("{id:int}")]
        public async Task<ActionResult<InstrumentRentalDto>> GetById(int id)
        {
            var dto = await _queryService.GetByIdForStudentAsync(id, CurrentUserId);
            return Ok(dto);
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<InstrumentRentalDto>>> GetMyRentals([FromQuery]InstrumentRentalSearchObject search)
        {
            var result = await _queryService.GetPagedForStudentAsync(CurrentUserId, search);
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<InstrumentRentalDto>> CreateRequest([FromBody]RentalCreateRequest request)
        {
            var dto = await _commandService.CreateRequestAsync(CurrentUserId, request);
            return Ok(dto);
        }

        [HttpPost("{id:int}/cancel")]
        public async Task<ActionResult<InstrumentRentalDto>> Cancel(int id, [FromBody]RentalStatusResponse response)
        {
            var dto = await _commandService.CancelAsync(id, CurrentUserId, response);
            return Ok(dto);
        }
    }
}
