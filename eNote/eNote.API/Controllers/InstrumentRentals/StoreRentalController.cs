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
    [Route("api/shop/rentals")]
    public sealed class StoreRentalController(IRentalQueryService queryService, IRentalCommandService commandService) : CoreController
    {
        private readonly IRentalQueryService _queryService = queryService;
        private readonly IRentalCommandService _commandService = commandService;

        [HttpGet("{id:int}")]
        public async Task<ActionResult<InstrumentRentalDto>> GetById(int id)
        {
            var dto = await _queryService.GetByIdForStoreAsync(id, CurrentUserId);
            return Ok(dto);
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<InstrumentRentalDto>>> GetShopRentals([FromQuery]InstrumentRentalSearchObject search)
        {
            var result = await _queryService.GetPagedForStoreAsync(CurrentUserId, search);
            return Ok(result);
        }

        [HttpPost("{id:int}/approve")]
        public async Task<ActionResult<InstrumentRentalDto>> Approve(int id, [FromBody]RentalStatusResponse response)
        {
            var dto = await _commandService.ApproveAsync(id, CurrentUserId, response);
            return Ok(dto);
        }

        [HttpPost("{id:int}/reject")]
        public async Task<ActionResult<InstrumentRentalDto>> Reject(int id, [FromBody]RentalStatusResponse response)
        {
            var dto = await _commandService.RejectAsync(id, CurrentUserId, response);
            return Ok(dto);
        }

        [HttpPost("{id:int}/pickup")]
        public async Task<ActionResult<InstrumentRentalDto>> Pickup(int id, [FromBody]RentalStatusResponse response)
        {
            var dto = await _commandService.PickupAsync(id, CurrentUserId, response);
            return Ok(dto);
        }

        [HttpPost("{id:int}/complete")]
        public async Task<ActionResult<InstrumentRentalDto>> Complete(int id, [FromBody]RentalStatusResponse response)
        {
            var dto = await _commandService.CompleteAsync(id, CurrentUserId, response);
            return Ok(dto);
        }

        [HttpPost("{id:int}/return-early")]
        public async Task<ActionResult<InstrumentRentalDto>> ReturnEarly(int id, [FromBody]RentalStatusResponse response)
        {
            var dto = await _commandService.ReturnEarlyAsync(id, CurrentUserId, response);
            return Ok(dto);
        }
    }
}
