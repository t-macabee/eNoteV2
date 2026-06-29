using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Rentals.InstrumentRentals;
using eNote.Application.Features.Rentals.InstrumentRentals.Services;
using eNote.Application.Features.Reports.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.InstrumentRentals;

[Authorize(Roles = AppRoles.StoreEmployee)]
[Route("api/v{version:apiVersion}/shop/rentals")]
public sealed class StoreRentalController(IRentalQueryService queryService, IRentalCommandService commandService, IReportService reportService) : CoreController
{
    [HttpGet("report")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRentalReport(CancellationToken cancellationToken)
    {
        var pdf = await reportService.GenerateStoreRentalSummaryPdfAsync(cancellationToken);
        return File(pdf, "application/pdf", "store-rentals.pdf");
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<InstrumentRentalDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<InstrumentRentalDto>>> GetPaged([FromQuery] InstrumentRentalSearchObject search, CancellationToken cancellationToken)
    {
        var result = await queryService.GetPagedForStoreAsync(search, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(InstrumentRentalDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstrumentRentalDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var dto = await queryService.GetByIdForStoreAsync(id, cancellationToken);
        return Ok(dto);
    }

    [HttpPost("{id:int}/approve")]
    [ProducesResponseType(typeof(InstrumentRentalDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstrumentRentalDto>> Approve(int id, [FromBody] RentalStatusResponse response, CancellationToken cancellationToken)
    {
        var dto = await commandService.ApproveAsync(id, response, cancellationToken);
        return Ok(dto);
    }

    [HttpPost("{id:int}/reject")]
    [ProducesResponseType(typeof(InstrumentRentalDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstrumentRentalDto>> Reject(int id, [FromBody] RentalStatusResponse response, CancellationToken cancellationToken)
    {
        var dto = await commandService.RejectAsync(id, response, cancellationToken);
        return Ok(dto);
    }

    [HttpPost("{id:int}/pickup")]
    [ProducesResponseType(typeof(InstrumentRentalDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstrumentRentalDto>> Pickup(int id, [FromBody] RentalStatusResponse response, CancellationToken cancellationToken)
    {
        var dto = await commandService.PickupAsync(id, response, cancellationToken);
        return Ok(dto);
    }

    [HttpPost("{id:int}/complete")]
    [ProducesResponseType(typeof(InstrumentRentalDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstrumentRentalDto>> Complete(int id, [FromBody] RentalStatusResponse response, CancellationToken cancellationToken)
    {
        var dto = await commandService.CompleteAsync(id, response, cancellationToken);
        return Ok(dto);
    }

    [HttpPost("{id:int}/return-early")]
    [ProducesResponseType(typeof(InstrumentRentalDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstrumentRentalDto>> ReturnEarly(int id, [FromBody] RentalStatusResponse response, CancellationToken cancellationToken)
    {
        var dto = await commandService.ReturnEarlyAsync(id, response, cancellationToken);
        return Ok(dto);
    }
}
