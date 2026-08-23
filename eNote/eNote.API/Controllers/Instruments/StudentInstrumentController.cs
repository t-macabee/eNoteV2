using eNote.API.Controllers.Base;
using eNote.Application.Constants;
using eNote.Application.Features.Rentals.Recommendations;
using eNote.Application.Features.Rentals.Recommendations.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Instruments;

[Authorize(Roles = AppRoles.Student)]
[Route("api/v{version:apiVersion}/student/instruments")]
public sealed class StudentInstrumentController(RecommendationService recommendationService) : CoreController
{
    [HttpGet("recommended")]
    [ProducesResponseType(typeof(IReadOnlyList<InstrumentRecommendationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<InstrumentRecommendationDto>>> GetRecommended([FromQuery] int count = 5, CancellationToken cancellationToken = default)
    {
        var result = await recommendationService.GetRecommendedInstrumentsAsync(count, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:int}/view")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RecordView(int id, CancellationToken cancellationToken)
    {
        await recommendationService.RecordInstrumentViewAsync(id, cancellationToken);
        return NoContent();
    }
}
