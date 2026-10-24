using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Identity.Students;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Admin;

[Authorize(Roles = AppRoles.Administrator)]
[Route("api/v{version:apiVersion}/admin/students")]
public sealed class AdminStudentController(AdminStudentService service, MembershipService membershipService) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<StudentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<StudentDto>>> GetPaged([FromQuery] StudentSearchObject search, CancellationToken cancellationToken)
    {
        PagedResult<StudentDto> result = await service.GetPagedAsync(search, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(StudentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<StudentDto>> GetById(int id, CancellationToken cancellationToken)
    {
        StudentDto dto = await service.GetByIdAsync(id, cancellationToken);
        return Ok(dto);
    }

    [HttpGet("{id:int}/membership-payments")]
    [ProducesResponseType(typeof(PagedResult<MembershipPaymentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<MembershipPaymentDto>>> GetMembershipPayments(
        int id,
        [FromQuery] MembershipPaymentSearchObject search,
        CancellationToken cancellationToken)
    {
        PagedResult<MembershipPaymentDto> result = await membershipService.GetPaymentsAsync(null, id, search, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:int}/membership-payments")]
    [ProducesResponseType(typeof(MembershipPaymentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MembershipPaymentDto>> RecordMembershipPayment(
        int id,
        [FromBody] RecordMembershipPaymentRequest request,
        CancellationToken cancellationToken)
    {
        MembershipPaymentDto payment = await membershipService.RecordPaymentAsync(null, id, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, payment);
    }

    [HttpPost("{id:int}/membership-warning")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendMembershipWarning(
        int id,
        [FromBody] SendMembershipWarningRequest request,
        CancellationToken cancellationToken)
    {
        await membershipService.SendWarningAsync(null, id, request, cancellationToken);
        return NoContent();
    }
}
