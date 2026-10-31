using eNote.API.Controllers.Academic;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Academic.Tuition;
using eNote.Application.Features.Payments;
using eNote.Tests.TestUtils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reflection;

namespace eNote.Tests.Academic;

public sealed class TuitionPaymentsControllerTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Controller_HasExpectedRouteAndAuthorizeAttributes()
    {
        var authAttr = typeof(TuitionPaymentsController).GetCustomAttribute<AuthorizeAttribute>(inherit: false);
        Assert.NotNull(authAttr);
        Assert.Equal(AppRoles.Student, authAttr.Roles);

        var routeAttr = typeof(TuitionPaymentsController).GetCustomAttribute<RouteAttribute>(inherit: false);
        Assert.NotNull(routeAttr);
        Assert.Equal("api/v{version:apiVersion}/student/enrollments/{enrollmentId:int}/tuition", routeAttr.Template);
    }

    [Fact]
    public async Task CreateIntent_ReturnsOkResult_WithResponse()
    {
        var (context, student, _, enrollment) = await TuitionTestData.SetupScenarioAsync(Now, price: 150m);
        var controller = new TuitionPaymentsController(CreateService(context, student));

        var result = await controller.CreateIntent(enrollment.Id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<CreateTuitionIntentResponse>(ok.Value);
        Assert.Equal(enrollment.Id, response.EnrollmentId);
        Assert.Equal(15000, response.AmountCents);
        Assert.Equal("bam", response.Currency);
        Assert.Equal(PaymentStatus.RequiresAction, response.Status);
        Assert.False(string.IsNullOrWhiteSpace(response.ClientSecret));
    }

    [Fact]
    public async Task GetLatest_ReturnsOkResult_WithDto()
    {
        var (context, student, _, enrollment) = await TuitionTestData.SetupScenarioAsync(Now, price: 150m);
        context.Set<CoursePayment>().Add(new CoursePayment(enrollment.Id, "pi_123", 15000, "bam", PaymentStatus.Succeeded));
        await context.SaveChangesAsync();
        var controller = new TuitionPaymentsController(CreateService(context, student));

        var result = await controller.GetLatest(enrollment.Id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<CoursePaymentDto>(ok.Value);
        Assert.Equal(enrollment.Id, dto.EnrollmentId);
        Assert.Equal("pi_123", dto.PaymentIntentId);
        Assert.Equal(PaymentStatus.Succeeded, dto.Status);
    }

    [Fact]
    public async Task GetHistory_ReturnsOkResult_WithList()
    {
        var (context, student, _, enrollment) = await TuitionTestData.SetupScenarioAsync(Now, price: 150m);
        var older = new CoursePayment(enrollment.Id, "pi_123", 15000, "bam", PaymentStatus.Succeeded);
        var newer = new CoursePayment(enrollment.Id, "pi_456", 15000, "bam", PaymentStatus.Succeeded);
        context.Set<CoursePayment>().AddRange(older, newer);
        await context.SaveChangesAsync();

        older.CreatedAt = Now.AddDays(-30);
        newer.CreatedAt = Now;
        await context.SaveChangesAsync();
        var controller = new TuitionPaymentsController(CreateService(context, student));

        var result = await controller.GetHistory(
            enrollment.Id,
            new CoursePaymentSearchObject { Page = 1, PageSize = 1 },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var page = Assert.IsType<PagedResult<CoursePaymentDto>>(ok.Value);
        var item = Assert.Single(page.Items);
        Assert.Equal("pi_456", item.PaymentIntentId);
        Assert.Equal(2, page.TotalCount);
    }

    private static TuitionPaymentService CreateService(ENoteContext context, Student student) =>
        new(
            context,
            TestMapper.Create(),
            new FixedClock(Now),
            new StubCurrentActor(student: student),
            new FakePaymentGateway(),
            new PaymentOptions { Currency = "bam" },
            NullLogger<TuitionPaymentService>.Instance);
}
