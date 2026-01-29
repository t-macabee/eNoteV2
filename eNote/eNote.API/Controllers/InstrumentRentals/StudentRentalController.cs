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
    public class StudentRentalController(IRentalService rentalService) : CoreController
    {
        private readonly IRentalService _rentalService = rentalService;

        [HttpGet("{id:int}")]
        public async Task<ActionResult<InstrumentRentalDto>> GetById(int id)
        {
            var dto = await _rentalService.GetByIdForStudentAsync(id, CurrentUserId);
            return Ok(dto);
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<InstrumentRentalDto>>> GetMyRentals([FromQuery]InstrumentRentalSearchObject search)
        {
            var result = await _rentalService.GetPagedForStudentAsync(CurrentUserId, search);
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<InstrumentRentalDto>> CreateRequest([FromBody]RentalCreateRequest request)
        {
            var dto = await _rentalService.CreateRequestAsync(CurrentUserId, request);
            return Ok(dto);
        }

        [HttpPost("{id:int}/cancel")]
        public async Task<ActionResult<InstrumentRentalDto>> Cancel(int id, [FromBody]RentalStatusResponse response)
        {
            var dto = await _rentalService.CancelAsync(id, CurrentUserId, response);
            return Ok(dto);
        }

        [HttpPost("{id:int}/return-early")]
        public async Task<ActionResult<InstrumentRentalDto>> ReturnEarly(int id, [FromBody]RentalStatusResponse response)
        {
            var dto = await _rentalService.ReturnEarlyAsync(id, CurrentUserId, response);
            return Ok(dto);
        }
    }
}
