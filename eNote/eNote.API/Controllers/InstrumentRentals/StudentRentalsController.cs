using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.DTOs;
using eNote.Application.Interfaces.InstrumentRentals;
using eNote.Application.Requests.InstrumentRental;
using eNote.Application.SearchObjects;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.InstrumentRentals
{
    [Route("api/student/rentals")]
    public class StudentRentalsController(IRentalService rentalService) : CoreController
    {
        private readonly IRentalService _rentalService = rentalService;

        [HttpGet("{id:int}")]
        public async Task<ActionResult<InstrumentRentalDto>> GetById(int id)
        {
            var dto = await _rentalService.GetByIdForStudentAsync(id, CurrentUserId);
            return Ok(dto);
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<InstrumentRentalDto>>> GetMyRentals([FromQuery] InstrumentRentalSearchObject search)
        {
            var result = await _rentalService.GetPagedForStudentAsync(CurrentUserId, search);
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<InstrumentRentalDto>> CreateRequest([FromBody] RentalCreateRequest request)
        {
            var dto = await _rentalService.CreateRequestAsync(CurrentUserId, request);
            return Ok(dto);
        }
    }
}
