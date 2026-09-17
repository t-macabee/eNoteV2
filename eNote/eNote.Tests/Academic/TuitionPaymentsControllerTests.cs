using eNote.API.Controllers.Tuition;
using eNote.Application.Constants;
using eNote.Application.Features.Academic.Tuition;
using eNote.Application.Features.Rentals.Payments.Services;
using eNote.Domain.Entities.Academic;
using eNote.Domain.Enums;
using eNote.Tests.TestUtils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reflection;

namespace eNote.Tests.Academic;

public sealed class TuitionPaymentsControllerTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Controller_ActionDescriptors_HaveExpectedRoutesAndHttpMethods()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddControllers().AddApplicationPart(typeof(TuitionPaymentsController).Assembly);
        var sp = services.BuildServiceProvider();
        var provider = sp.GetRequiredService<IActionDescriptorCollectionProvider>();

        var actions = provider.ActionDescriptors.Items
            .OfType<ControllerActionDescriptor>()
            .Where(a => a.ControllerTypeInfo == typeof(TuitionPaymentsController))
            .ToList();

        Assert.Equal(3, actions.Count);

        var createIntent = actions.Single(a => a.ActionName == nameof(TuitionPaymentsController.CreateIntent));
        Assert.Equal("POST", Assert.Single(createIntent.ActionConstraints?.OfType<HttpMethodActionConstraint>().SelectMany(c => c.HttpMethods) ?? ["POST"]));
        Assert.Equal("api/v{version:apiVersion}/student/enrollments/{enrollmentId:int}/tuition/create-intent", createIntent.AttributeRouteInfo?.Template);

        var getLatest = actions.Single(a => a.ActionName == nameof(TuitionPaymentsController.GetLatest));
        Assert.Equal("GET", Assert.Single(getLatest.ActionConstraints?.OfType<HttpMethodActionConstraint>().SelectMany(c => c.HttpMethods) ?? ["GET"]));
        Assert.Equal("api/v{version:apiVersion}/student/enrollments/{enrollmentId:int}/tuition", getLatest.AttributeRouteInfo?.Template);

        var getHistory = actions.Single(a => a.ActionName == nameof(TuitionPaymentsController.GetHistory));
        Assert.Equal("GET", Assert.Single(getHistory.ActionConstraints?.OfType<HttpMethodActionConstraint>().SelectMany(c => c.HttpMethods) ?? ["GET"]));
        Assert.Equal("api/v{version:apiVersion}/student/enrollments/{enrollmentId:int}/tuition/history", getHistory.AttributeRouteInfo?.Template);
    }
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
        var older = new CoursePayment(enrollment.Id, "pi_123", 15000, "bam", PaymentStatus.Succeeded) { CreatedAt = Now.AddDays(-30) };
        var newer = new CoursePayment(enrollment.Id, "pi_456", 15000, "bam", PaymentStatus.Succeeded) { CreatedAt = Now };
        context.Set<CoursePayment>().AddRange(older, newer);
        await context.SaveChangesAsync();
        var controller = new TuitionPaymentsController(CreateService(context, student));

        var result = await controller.GetHistory(enrollment.Id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IReadOnlyList<CoursePaymentDto>>(ok.Value);
        Assert.Equal(2, list.Count);
        Assert.Equal("pi_456", list[0].PaymentIntentId);
        Assert.Equal("pi_123", list[1].PaymentIntentId);
    }

    private static TuitionPaymentService CreateService(ENoteContext context, Student student) =>
        new(
            context,
            TestMapper.Create(),
            new FixedClock(Now),
            new StubCurrentActor(student: student),
            new FakePaymentGateway(),
            new StripeOptions { Currency = "bam", StatementDescriptor = "ENOTE" },
            NullLogger<TuitionPaymentService>.Instance);
}
