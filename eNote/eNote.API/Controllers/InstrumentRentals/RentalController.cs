using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Rentals.InstrumentRentals;
using eNote.Application.Features.Rentals.InstrumentRentals.Services;
using eNote.Application.Features.Reports.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.InstrumentRentals;

[Route("api/v{version:apiVersion}/shop/rentals")]
[Route("api/v{version:apiVersion}/student/rentals")]
public sealed class RentalController(
    RentalQueryService queryService,
    RentalCommandService commandService,
    IReportService reportService) : CoreController
{
    // ── Store employee actions ──────────────────────────────────────

    [Authorize(Roles = AppRoles.StoreEmployee)]
    [HttpGet("~/api/v{version:apiVersion}/shop/rentals/report")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRentalReport(CancellationToken cancellationToken)
    {
        var pdf = await reportService.GenerateStoreRentalSummaryPdfAsync(cancellationToken);
        return File(pdf, "application/pdf", "store-rentals.pdf");
    }

    [Authorize(Roles = AppRoles.StoreEmployee)]
    [HttpGet("~/api/v{version:apiVersion}/shop/rentals")]
    [ProducesResponseType(typeof(PagedResult<InstrumentRentalDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<InstrumentRentalDto>>> GetPagedForStore([FromQuery] InstrumentRentalSearchObject search, CancellationToken cancellationToken)
    {
        var result = await queryService.GetPagedForStoreAsync(search, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = AppRoles.StoreEmployee)]
    [HttpGet("~/api/v{version:apiVersion}/shop/rentals/{id:int}")]
    [ProducesResponseType(typeof(InstrumentRentalDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstrumentRentalDto>> GetByIdForStore(int id, CancellationToken cancellationToken)
    {
        var dto = await queryService.GetByIdForStoreAsync(id, cancellationToken);
        return Ok(dto);
    }

    [Authorize(Roles = AppRoles.StoreEmployee)]
    [HttpPost("~/api/v{version:apiVersion}/shop/rentals/{id:int}/approve")]
    [ProducesResponseType(typeof(InstrumentRentalDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstrumentRentalDto>> Approve(int id, [FromBody] RentalStatusRequest? request, CancellationToken cancellationToken)
    {
        var dto = await commandService.ApproveAsync(id, request, cancellationToken);
        return Ok(dto);
    }

    [Authorize(Roles = AppRoles.StoreEmployee)]
    [HttpPost("~/api/v{version:apiVersion}/shop/rentals/{id:int}/reject")]
    [ProducesResponseType(typeof(InstrumentRentalDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstrumentRentalDto>> Reject(int id, [FromBody] RentalStatusRequest? request, CancellationToken cancellationToken)
    {
        var dto = await commandService.RejectAsync(id, request, cancellationToken);
        return Ok(dto);
    }

    [Authorize(Roles = AppRoles.StoreEmployee)]
    [HttpPost("~/api/v{version:apiVersion}/shop/rentals/{id:int}/pickup")]
    [ProducesResponseType(typeof(InstrumentRentalDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstrumentRentalDto>> Pickup(int id, [FromBody] RentalStatusRequest? request, CancellationToken cancellationToken)
    {
        var dto = await commandService.PickupAsync(id, request, cancellationToken);
        return Ok(dto);
    }

    [Authorize(Roles = AppRoles.StoreEmployee)]
    [HttpPost("~/api/v{version:apiVersion}/shop/rentals/{id:int}/complete")]
    [ProducesResponseType(typeof(InstrumentRentalDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstrumentRentalDto>> Complete(int id, [FromBody] RentalStatusRequest? request, CancellationToken cancellationToken)
    {
        var dto = await commandService.CompleteAsync(id, request, cancellationToken);
        return Ok(dto);
    }

    [Authorize(Roles = AppRoles.StoreEmployee)]
    [HttpPost("~/api/v{version:apiVersion}/shop/rentals/{id:int}/return-early")]
    [ProducesResponseType(typeof(InstrumentRentalDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstrumentRentalDto>> ReturnEarly(int id, [FromBody] RentalStatusRequest? request, CancellationToken cancellationToken)
    {
        var dto = await commandService.ReturnEarlyAsync(id, request, cancellationToken);
        return Ok(dto);
    }

    // ── Student actions ─────────────────────────────────────────────

    [Authorize(Roles = AppRoles.Student)]
    [HttpGet("~/api/v{version:apiVersion}/student/rentals")]
    [ProducesResponseType(typeof(PagedResult<InstrumentRentalDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<InstrumentRentalDto>>> GetPagedForStudent([FromQuery] InstrumentRentalSearchObject search, CancellationToken cancellationToken)
    {
        var result = await queryService.GetPagedForStudentAsync(search, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = AppRoles.Student)]
    [HttpGet("~/api/v{version:apiVersion}/student/rentals/{id:int}")]
    [ProducesResponseType(typeof(InstrumentRentalDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstrumentRentalDto>> GetByIdForStudent(int id, CancellationToken cancellationToken)
    {
        var dto = await queryService.GetByIdForStudentAsync(id, cancellationToken);
        return Ok(dto);
    }

    [Authorize(Roles = AppRoles.Student)]
    [HttpPost("~/api/v{version:apiVersion}/student/rentals")]
    [ProducesResponseType(typeof(InstrumentRentalDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<InstrumentRentalDto>> CreateRequest([FromBody] RentalCreateRequest request, CancellationToken cancellationToken)
    {
        var dto = await commandService.CreateRequestAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetByIdForStudent), new { id = dto.Id }, dto);
    }

    [Authorize(Roles = AppRoles.Student)]
    [HttpPost("~/api/v{version:apiVersion}/student/rentals/{id:int}/cancel")]
    [ProducesResponseType(typeof(InstrumentRentalDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstrumentRentalDto>> Cancel(int id, [FromBody] RentalStatusRequest? request, CancellationToken cancellationToken)
    {
        var dto = await commandService.CancelAsync(id, request, cancellationToken);
        return Ok(dto);
    }
}
