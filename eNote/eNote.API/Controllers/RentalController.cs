using eNote.Application;
using eNote.Application.DTOs;
using eNote.Application.Interfaces.Instruments.InstrumentRentals;
using eNote.Application.Requests.InstrumentRental;
using eNote.Application.SearchObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace eNote.API.Controllers
{
    [ApiController]
    [Route("api/rentals")]
    //[Authorize]  
    public class RentalsController : ControllerBase
    {
        private readonly IRentalService _rentalService;

        public RentalsController(IRentalService rentalService)
        {
            _rentalService = rentalService;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<InstrumentRentalDto>> GetById(int id)
        {
            var userId = GetCurrentUserId();  

            var dto = await _rentalService.GetByIdForStudentAsync(id, userId);

            return Ok(dto);
        }

        [HttpGet("my")]
        public async Task<ActionResult<PagedResult<InstrumentRentalDto>>> GetMyRentals([FromQuery] InstrumentRentalSearchObject search)
        {
            var userId = GetCurrentUserId();

            var result = await _rentalService.GetPagedForStudentAsync(userId, search);

            return Ok(result);
        }

        [HttpGet("shop")]
        //[Authorize(Roles = "MusicShop")]  
        public async Task<ActionResult<PagedResult<InstrumentRentalDto>>> GetShopRentals([FromQuery] InstrumentRentalSearchObject search)
        {
            var userId = GetCurrentUserId();

            var result = await _rentalService.GetPagedForShopAsync(userId, search);

            return Ok(result);
        }        

        [HttpPost]
        public async Task<ActionResult<InstrumentRentalDto>> CreateRequest([FromBody] RentalCreateRequest request)
        {
            var studentId = GetCurrentUserId();  

            var dto = await _rentalService.CreateRequestAsync(studentId, request);

            return Ok(dto);
        }

        [HttpPost("{id}/approve")]
        //[Authorize(Roles = "MusicShop")]
        public async Task<ActionResult<InstrumentRentalDto>> Approve(int id, [FromBody] RentalStatusResponse response)
        {
            var shopUserId = GetCurrentUserId();

            var dto = await _rentalService.ApproveAsync(id, shopUserId, response);

            return Ok(dto);
        }

        [HttpPost("{id}/reject")]
        //[Authorize(Roles = "MusicShop")]
        public async Task<ActionResult<InstrumentRentalDto>> Reject(int id, [FromBody] RentalStatusResponse response)
        {
            var shopUserId = GetCurrentUserId();

            var dto = await _rentalService.RejectAsync(id, shopUserId, response);

            return Ok(dto);
        }

        [HttpPost("{id}/pickup")]
        //[Authorize(Roles = "MusicShop")]
        public async Task<ActionResult<InstrumentRentalDto>> Pickup(int id, [FromBody] RentalStatusResponse response)
        {
            var shopUserId = GetCurrentUserId();

            var dto = await _rentalService.PickupAsync(id, shopUserId, response);

            return Ok(dto);
        }

        [HttpPost("{id}/complete")]
        //[Authorize(Roles = "MusicShop")]
        public async Task<ActionResult<InstrumentRentalDto>> Complete(int id, [FromBody] RentalStatusResponse response)
        {
            var shopUserId = GetCurrentUserId();

            var dto = await _rentalService.CompleteAsync(id, shopUserId, response);

            return Ok(dto);
        }
                
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("sub")?.Value
                           ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("User ID not found in token.");
            }

            return userId;
        }
    }
}
