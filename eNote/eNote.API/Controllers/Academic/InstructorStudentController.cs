using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Identity.Students;
using eNote.Application.Features.Identity.Users;
using eNote.Application.Features.Identity.Users.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Academic;

[Authorize(Roles = AppRoles.Instructor)]
[Route("api/v{version:apiVersion}/instructor/students")]
public sealed class InstructorStudentController(
    AdminStudentService studentService,
    MembershipService membershipService,
    IUserProvisioningService provisioningService) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<StudentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<StudentDto>>> GetPaged(
        [FromQuery] StudentSearchObject search,
        CancellationToken cancellationToken)
    {
        var result = await studentService.GetPagedForCurrentInstructorAsync(search, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(StudentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<StudentDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var dto = await studentService.GetByIdForCurrentInstructorAsync(id, cancellationToken);
        return Ok(dto);
    }

    [HttpGet("{id:int}/enrollments")]
    [ProducesResponseType(typeof(PagedResult<StudentEnrollmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<StudentEnrollmentDto>>> GetEnrollments(
        int id,
        [FromQuery] StudentEnrollmentSearchObject search,
        CancellationToken cancellationToken)
    {
        var result = await studentService.GetEnrollmentsForCurrentInstructorAsync(id, search, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}/membership-payments")]
    [ProducesResponseType(typeof(PagedResult<MembershipPaymentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<MembershipPaymentDto>>> GetMembershipPayments(
        int id,
        [FromQuery] MembershipPaymentSearchObject search,
        CancellationToken cancellationToken)
    {
        var result = await membershipService.GetPaymentsForCurrentInstructorAsync(id, search, cancellationToken);
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
        var payment = await membershipService.RecordPaymentForCurrentInstructorAsync(id, request, cancellationToken);
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
        await membershipService.SendWarningForCurrentInstructorAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserCreatedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserCreatedResponse>> Create(
        [FromBody] DelegatedUserCreateRequest request,
        CancellationToken cancellationToken)
    {
        var userId = await provisioningService.ProvisionStudentByInstructorAsync(request, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new UserCreatedResponse(userId));
    }
}
