using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.DTOs;
using eNote.Application.Interfaces.InstrumentRentals;
using eNote.Application.Requests.InstrumentRental;
using eNote.Application.SearchObjects;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.InstrumentRentals
{
    [Route("api/shop/rentals")]
    public sealed class ShopRentalsController(IRentalService rentalService) : CoreController
    {
        private readonly IRentalService _rentalService = rentalService;

        [HttpGet("{id:int}")]
        public async Task<ActionResult<InstrumentRentalDto>> GetById(int id)
        {
            var dto = await _rentalService.GetByIdForShopAsync(id, CurrentUserId);
            return Ok(dto);
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<InstrumentRentalDto>>> GetShopRentals([FromQuery] InstrumentRentalSearchObject search)
        {
            var result = await _rentalService.GetPagedForShopAsync(CurrentUserId, search);
            return Ok(result);
        }

        [HttpPost("{id:int}/approve")]
        public async Task<ActionResult<InstrumentRentalDto>> Approve(int id, [FromBody] RentalStatusResponse response)
        {
            var dto = await _rentalService.ApproveAsync(id, CurrentUserId, response);
            return Ok(dto);
        }

        [HttpPost("{id:int}/reject")]
        public async Task<ActionResult<InstrumentRentalDto>> Reject(int id, [FromBody] RentalStatusResponse response)
        {
            var dto = await _rentalService.RejectAsync(id, CurrentUserId, response);
            return Ok(dto);
        }

        [HttpPost("{id:int}/pickup")]
        public async Task<ActionResult<InstrumentRentalDto>> Pickup(int id, [FromBody] RentalStatusResponse response)
        {
            var dto = await _rentalService.PickupAsync(id, CurrentUserId, response);
            return Ok(dto);
        }

        [HttpPost("{id:int}/complete")]
        public async Task<ActionResult<InstrumentRentalDto>> Complete(int id, [FromBody] RentalStatusResponse response)
        {
            var dto = await _rentalService.CompleteAsync(id, CurrentUserId, response);
            return Ok(dto);
        }
    }
}
