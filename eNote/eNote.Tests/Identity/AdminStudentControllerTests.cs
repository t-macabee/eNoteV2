using eNote.API.Controllers.Admin;
using eNote.Application.Common.Paging;
using eNote.Application.Features.Identity.Students;
using eNote.Application.Features.Identity.Instructors;
using eNote.Tests.TestUtils;
using Microsoft.AspNetCore.Mvc;

namespace eNote.Tests.Identity;

public sealed class AdminStudentControllerTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task RecordMembershipPayment_ReturnsCreated_ForAStudentOutsideAnyCourse()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var student = new Student(10, Now);
        context.Set<Student>().Add(student);
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        var result = await controller.RecordMembershipPayment(student.Id, new RecordMembershipPaymentRequest
        {
            Amount = 40m,
            ReceiptNumber = "CL-200",
            Months = 1
        }, CancellationToken.None);

        var created = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(201, created.StatusCode);
        var payment = Assert.IsType<MembershipPaymentDto>(created.Value);
        Assert.Equal("CL-200", payment.ReceiptNumber);
        Assert.Equal(Now.Date, payment.PeriodStart);
    }

    [Fact]
    public async Task GetMembershipPayments_ReturnsEmptyPage_WhenNothingIsRecorded()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var student = new Student(10, Now);
        context.Set<Student>().Add(student);
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        var result = await controller.GetMembershipPayments(student.Id, new MembershipPaymentSearchObject(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var paged = Assert.IsType<PagedResult<MembershipPaymentDto>>(ok.Value);
        Assert.Empty(paged.Items);
    }

    private static AdminStudentController CreateController(ENoteContext context)
    {
        var instructorAccess = new InstructorAccessService(context, new StubUserProfileLookup());
        var studentService = new AdminStudentService(context, new StubUserIdentityService(), instructorAccess);
        var membershipService = new MembershipService(
            context,
            studentService,
            new FixedClock(Now),
            new RecordingMembershipWarningDispatcher());

        return new AdminStudentController(studentService, membershipService);
    }
}
