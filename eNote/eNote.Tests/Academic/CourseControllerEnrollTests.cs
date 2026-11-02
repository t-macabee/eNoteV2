using eNote.API.Controllers.Academic;
using eNote.Application.Features.Academic.Courses;
using eNote.Application.Features.Academic.Courses.Services;
using eNote.Tests.TestUtils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace eNote.Tests.Academic;

public sealed class CourseControllerEnrollTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Enroll_ReturnsActive_WhenCanceledEnrollmentHasPaidTimeLeft()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var harness = await AcademicTestData.SeedAsync(context, Now);
        harness.Student.UpdateMembership(Now.AddDays(30));
        var enrollment = await context.Set<Enrollment>()
            .SingleAsync(e => e.StudentId == harness.Student.Id && e.CourseId == harness.Course.Id);
        enrollment.Transition(EnrollmentTrigger.Cancel, userId: 1, now: Now);
        await context.SaveChangesAsync();
        var controller = CreateController(context, harness);

        var result = await controller.Enroll(harness.Course.Id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<EnrollResultDto>(ok.Value);
        Assert.Equal(EnrollmentStatus.Active, dto.EnrollmentStatus);
    }

    private static CourseController CreateController(ENoteContext context, AcademicHarness harness)
    {
        var actor = new StubCurrentActor(student: harness.Student);
        var instructorAccess = AcademicTestData.CreateInstructorAccess(context, harness.Instructor);
        var clock = new FixedClock(Now);

        var courseService = new CourseService(
            context,
            TestMapper.Create(),
            actor,
            actor,
            instructorAccess,
            NullLogger<CourseService>.Instance,
            new StubUserIdentityService(),
            new FakePaymentGateway(),
            clock);

        var enrollmentService = new CourseEnrollmentService(
            context,
            clock,
            actor,
            actor,
            new FakePaymentGateway(),
            NullLogger<CourseEnrollmentService>.Instance);

        return new CourseController(courseService, enrollmentService);
    }
}
