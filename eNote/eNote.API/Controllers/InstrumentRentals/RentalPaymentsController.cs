using eNote.API.Controllers.Base;
using eNote.Application.Constants;
using eNote.Application.Features.Rentals.Payments;
using eNote.Application.Features.Rentals.Payments.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.InstrumentRentals;

/// <summary>
/// Student-facing payment-intent creation/status and store-facing refunds.
/// Follows RentalController's two-prefix convention: student routes live under
/// /student/rentals, the store refund route is an absolute /shop/rentals route.
/// </summary>
[Route("api/v{version:apiVersion}/student/rentals/{rentalId:int}/payments")]
public sealed class RentalPaymentsController(IRentalPaymentService payments) : CoreController
{
    [Authorize(Roles = AppRoles.Student)]
    [HttpPost("create-intent")]
    [ProducesResponseType(typeof(CreatePaymentIntentResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CreatePaymentIntentResponse>> CreateIntent(int rentalId, CancellationToken cancellationToken)
    {
        var result = await payments.CreatePaymentIntentAsync(rentalId, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = AppRoles.Student)]
    [HttpGet]
    [ProducesResponseType(typeof(RentalPaymentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<RentalPaymentDto>> GetStatus(int rentalId, CancellationToken cancellationToken)
    {
        var result = await payments.GetPaymentStatusAsync(rentalId, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = AppRoles.StoreEmployee)]
    [HttpPost("~/api/v{version:apiVersion}/shop/rentals/{rentalId:int}/payments/refund")]
    [ProducesResponseType(typeof(RentalPaymentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<RentalPaymentDto>> Refund(int rentalId, [FromBody] RefundRequest? request, CancellationToken cancellationToken)
    {
        var result = await payments.RefundAsync(rentalId, request?.AmountCents, cancellationToken);
        return Ok(result);
    }
}
