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
    [Authorize(Roles = AppRoles.StoreEmployee)]
    [Route("api/shop/rentals")]
    public sealed class StoreRentalController(IRentalQueryService queryService, IRentalCommandService commandService) : CoreController
    {
        [HttpGet]
        public async Task<ActionResult<PagedResult<InstrumentRentalDto>>> GetPaged([FromQuery] InstrumentRentalSearchObject search)
        {
            var result = await queryService.GetPagedForStoreAsync(search);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<InstrumentRentalDto>> GetById(int id)
        {
            var dto = await queryService.GetByIdForStoreAsync(id);
            return Ok(dto);
        }

        [HttpPost("{id:int}/approve")]
        public async Task<ActionResult<InstrumentRentalDto>> Approve(int id, [FromBody] RentalStatusResponse response)
        {
            var dto = await commandService.ApproveAsync(id, response);
            return Ok(dto);
        }

        [HttpPost("{id:int}/reject")]
        public async Task<ActionResult<InstrumentRentalDto>> Reject(int id, [FromBody] RentalStatusResponse response)
        {
            var dto = await commandService.RejectAsync(id, response);
            return Ok(dto);
        }

        [HttpPost("{id:int}/pickup")]
        public async Task<ActionResult<InstrumentRentalDto>> Pickup(int id, [FromBody] RentalStatusResponse response)
        {
            var dto = await commandService.PickupAsync(id, response);
            return Ok(dto);
        }

        [HttpPost("{id:int}/complete")]
        public async Task<ActionResult<InstrumentRentalDto>> Complete(int id, [FromBody] RentalStatusResponse response)
        {
            var dto = await commandService.CompleteAsync(id, response);
            return Ok(dto);
        }

        [HttpPost("{id:int}/return-early")]
        public async Task<ActionResult<InstrumentRentalDto>> ReturnEarly(int id, [FromBody] RentalStatusResponse response)
        {
            var dto = await commandService.ReturnEarlyAsync(id, response);
            return Ok(dto);
        }
    }
}
