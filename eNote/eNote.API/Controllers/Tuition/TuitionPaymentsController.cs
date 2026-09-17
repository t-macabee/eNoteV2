using eNote.API.Controllers.Base;
using eNote.Application.Constants;
using eNote.Application.Features.Academic.Tuition;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Tuition;

[Authorize(Roles = AppRoles.Student)]
[Route("api/v{version:apiVersion}/student/enrollments/{enrollmentId:int}/tuition")]
public sealed class TuitionPaymentsController(TuitionPaymentService tuitionService) : CoreController
{
    [HttpPost("create-intent")]
    [ProducesResponseType(typeof(CreateTuitionIntentResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CreateTuitionIntentResponse>> CreateIntent(int enrollmentId, CancellationToken cancellationToken)
    {
        var result = await tuitionService.CreateIntentAsync(enrollmentId, cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(CoursePaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CoursePaymentDto>> GetLatest(int enrollmentId, CancellationToken cancellationToken)
    {
        var result = await tuitionService.GetLatestAsync(enrollmentId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("history")]
    [ProducesResponseType(typeof(IReadOnlyList<CoursePaymentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CoursePaymentDto>>> GetHistory(int enrollmentId, CancellationToken cancellationToken)
    {
        var result = await tuitionService.GetHistoryAsync(enrollmentId, cancellationToken);
        return Ok(result);
    }
}
