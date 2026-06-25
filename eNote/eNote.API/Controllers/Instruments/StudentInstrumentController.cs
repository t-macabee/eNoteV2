using eNote.API.Controllers.Base;
using eNote.Application.Constants;
using eNote.Application.Features.Recommendations;
using eNote.Application.Features.Recommendations.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Instruments;

[Authorize(Roles = AppRoles.Student)]
[Route("api/student/instruments")]
public sealed class StudentInstrumentController(IRecommendationService recommendationService) : CoreController
{
    [HttpGet("recommended")]
    [ProducesResponseType(typeof(IReadOnlyList<InstrumentRecommendationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<InstrumentRecommendationDto>>> GetRecommended([FromQuery] int count = 5)
    {
        var result = await recommendationService.GetRecommendedInstrumentsAsync(count);
        return Ok(result);
    }

    [HttpPost("{id:int}/view")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RecordView(int id)
    {
        await recommendationService.RecordInstrumentViewAsync(id);
        return NoContent();
    }
}
