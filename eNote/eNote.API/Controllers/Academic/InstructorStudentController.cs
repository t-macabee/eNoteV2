using eNote.API.Controllers.Base;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Identity.Instructors;
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
    IUserProvisioningService provisioningService,
    ICurrentUserContext currentUser,
    InstructorAccessService instructorAccess) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<StudentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<StudentDto>>> GetPaged(
        [FromQuery] StudentSearchObject search,
        CancellationToken cancellationToken)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId, cancellationToken);
        var result = await studentService.GetPagedForInstructorAsync(instructorId, search, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(StudentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<StudentDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId, cancellationToken);
        var dto = await studentService.GetByIdForInstructorAsync(instructorId, id, cancellationToken);
        return Ok(dto);
    }

    [HttpGet("{id:int}/enrollments")]
    [ProducesResponseType(typeof(PagedResult<StudentEnrollmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<StudentEnrollmentDto>>> GetEnrollments(
        int id,
        [FromQuery] StudentEnrollmentSearchObject search,
        CancellationToken cancellationToken)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId, cancellationToken);
        var result = await studentService.GetEnrollmentsForInstructorAsync(instructorId, id, search, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}/membership-payments")]
    [ProducesResponseType(typeof(PagedResult<MembershipPaymentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<MembershipPaymentDto>>> GetMembershipPayments(
        int id,
        [FromQuery] MembershipPaymentSearchObject search,
        CancellationToken cancellationToken)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId, cancellationToken);
        var result = await membershipService.GetPaymentsAsync(instructorId, id, search, cancellationToken);
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
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId, cancellationToken);
        var payment = await membershipService.RecordPaymentAsync(instructorId, id, request, cancellationToken);
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
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId, cancellationToken);
        await membershipService.SendWarningAsync(instructorId, id, request, cancellationToken);
        return NoContent();
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Create(
        [FromBody] DelegatedUserCreateRequest request,
        CancellationToken cancellationToken)
    {
        var userId = await provisioningService.ProvisionStudentByInstructorAsync(request, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new { userId });
    }
}
