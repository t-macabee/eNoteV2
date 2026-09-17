using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Application.Constants;
using eNote.Application.Features.Academic.Tuition;
using eNote.Application.Features.Rentals.Payments.Services;
using eNote.Domain.Entities.Academic;
using eNote.Domain.Enums;
using eNote.Infrastructure.Data;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace eNote.Tests.Academic;

public sealed class TuitionPaymentServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateIntentAsync_CalculatesAmountFromCoursePrice()
    {
        var (context, student, course, enrollment) = await SetupScenarioAsync(price: 120.50m);
        var gateway = new FakePaymentGateway();
        var service = CreateService(context, student, gateway);

        var result = await service.CreateIntentAsync(enrollment.Id);

        Assert.Equal(enrollment.Id, result.EnrollmentId);
        Assert.Equal(12050, result.AmountCents);
        Assert.Equal("bam", result.Currency);
        Assert.Equal(PaymentStatus.RequiresAction, result.Status);
        Assert.False(string.IsNullOrWhiteSpace(result.ClientSecret));

        var call = Assert.Single(gateway.CreateCalls);
        Assert.Equal(12050, call.AmountCents);
        Assert.Equal("bam", call.Currency);
        Assert.Equal(TuitionOptions.StatementDescriptorSuffix, call.StatementDescriptorSuffix);
        Assert.Equal(enrollment.Id.ToString(), call.Metadata["enrollmentId"]);
        Assert.Equal(course.Id.ToString(), call.Metadata["courseId"]);
        Assert.Equal(student.Id.ToString(), call.Metadata["studentId"]);
    }

    [Fact]
    public async Task CreateIntentAsync_FreeCourse_Throws_BusinessException()
    {
        var (context, student, _, enrollment) = await SetupScenarioAsync(price: 0m);
        var service = CreateService(context, student);

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.CreateIntentAsync(enrollment.Id));

        Assert.Equal(Messages.CourseIsFree, ex.Message);
    }

    [Fact]
    public async Task CreateIntentAsync_UnpublishedCourse_Throws_CourseNotPayable()
    {
        var (context, student, course, enrollment) = await SetupScenarioAsync(price: 100m);
        course.SetPublishedStatus(false);
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.CreateIntentAsync(enrollment.Id));

        Assert.Equal(Messages.CourseNotPayable, ex.Message);
    }

    [Fact]
    public async Task CreateIntentAsync_EndedCourse_Throws_CourseNotPayable()
    {
        var (context, student, course, enrollment) = await SetupScenarioAsync(price: 100m);
        course.UpdateDetails(course.Name, course.Description, course.Price, course.StartDate, Now.AddDays(-1));
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.CreateIntentAsync(enrollment.Id));

        Assert.Equal(Messages.CourseNotPayable, ex.Message);
    }

    [Fact]
    public async Task CreateIntentAsync_WrongStudent_Throws_NotFound()
    {
        var (context, _, _, enrollment) = await SetupScenarioAsync(price: 100m);
        var otherStudent = new Student(88, Now);
        context.Set<Student>().Add(otherStudent);
        await context.SaveChangesAsync();

        var service = CreateService(context, otherStudent);

        await Assert.ThrowsAsync<NotFoundException>(() => service.CreateIntentAsync(enrollment.Id));
    }

    [Fact]
    public async Task CreateIntentAsync_CanceledEnrollment_Throws_StudentNotEnrolled()
    {
        var (context, student, _, enrollment) = await SetupScenarioAsync(price: 100m);
        enrollment.UpdateStatus(EnrollmentStatus.Canceled);
        await context.SaveChangesAsync();

        var service = CreateService(context, student);

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.CreateIntentAsync(enrollment.Id));
        Assert.Equal(Messages.StudentNotEnrolled, ex.Message);
    }

    [Fact]
    public async Task CreateIntentAsync_ReusesRequiresActionIntent_Within30Minutes()
    {
        var (context, student, _, enrollment) = await SetupScenarioAsync(price: 100m);
        var gateway = new FakePaymentGateway();
        var clock = new MutableClock(Now);
        var service = CreateService(context, student, gateway, clock);

        var first = await service.CreateIntentAsync(enrollment.Id);
        clock.UtcNow = Now.AddMinutes(15);
        var second = await service.CreateIntentAsync(enrollment.Id);

        Assert.Equal(first.PaymentIntentId, second.PaymentIntentId);
        Assert.Single(gateway.CreateCalls);
        Assert.Single(gateway.RetrieveCalls);
        Assert.Equal(first.PaymentIntentId, gateway.RetrieveCalls[0].PaymentIntentId);
    }

    [Fact]
    public async Task CreateIntentAsync_CreatesNewIntent_After30Minutes()
    {
        var (context, student, _, enrollment) = await SetupScenarioAsync(price: 100m);
        var gateway = new FakePaymentGateway();
        var clock = new MutableClock(Now);
        var service = CreateService(context, student, gateway, clock);

        var first = await service.CreateIntentAsync(enrollment.Id);
        clock.UtcNow = Now.AddMinutes(31);
        var second = await service.CreateIntentAsync(enrollment.Id);

        Assert.NotEqual(first.PaymentIntentId, second.PaymentIntentId);
        Assert.Equal(2, gateway.CreateCalls.Count);
        Assert.NotEqual(gateway.CreateCalls[0].IdempotencyKey, gateway.CreateCalls[1].IdempotencyKey);
        Assert.Empty(gateway.RetrieveCalls);
    }

    [Fact]
    public async Task CreateIntentAsync_AfterFailedPayment_CreatesNewIntentWithNewKey()
    {
        var (context, student, _, enrollment) = await SetupScenarioAsync(price: 100m);
        var gateway = new FakePaymentGateway();
        var clock = new MutableClock(Now);
        var service = CreateService(context, student, gateway, clock);

        await service.CreateIntentAsync(enrollment.Id);
        var payment = context.Set<CoursePayment>().Single();
        payment.MarkFailed("evt_test_failed");
        await context.SaveChangesAsync();

        clock.UtcNow = Now.AddMinutes(5);
        var second = await service.CreateIntentAsync(enrollment.Id);

        Assert.NotEqual("pi_test_1", second.PaymentIntentId);
        Assert.Equal(2, gateway.CreateCalls.Count);
        Assert.NotEqual(gateway.CreateCalls[0].IdempotencyKey, gateway.CreateCalls[1].IdempotencyKey);
        Assert.Empty(gateway.RetrieveCalls);
    }

    [Fact]
    public async Task CreateIntentAsync_ConcurrentDuplicate_ReturnsExistingIntent()
    {
        var (context, student, _, enrollment) = await SetupScenarioAsync(price: 100m);
        var winner = new CoursePayment(enrollment.Id, "pi_test_1", 10000, "bam", PaymentStatus.RequiresAction);
        context.Set<CoursePayment>().Add(winner);
        await context.SaveChangesAsync();
        winner.CreatedAt = Now.AddHours(-1);
        await context.SaveChangesAsync();
        var gateway = new FakePaymentGateway();
        var inner = new Exception($"duplicate key value violates unique constraint \"{DbConstraintNames.CoursePaymentPaymentIntentIdUniqueIndex}\"");
        var throwing = new ThrowingSaveDbContext(context, new DbUpdateException("Unique constraint violated.", inner));
        var service = CreateService(throwing, student, gateway);

        var result = await service.CreateIntentAsync(enrollment.Id);

        Assert.Equal("pi_test_1", result.PaymentIntentId);
        Assert.Equal(enrollment.Id, result.EnrollmentId);
        Assert.Equal(10000, result.AmountCents);
        Assert.Equal("bam", result.Currency);
    }

    [Fact]
    public async Task CreateIntentAsync_IdempotencyKey_StableUntilPaid()
    {
        var (context, student, _, enrollment) = await SetupScenarioAsync(price: 100m);
        var gateway = new FakePaymentGateway();
        var clock = new MutableClock(Now);
        var service = CreateService(context, student, gateway, clock);

        await service.CreateIntentAsync(enrollment.Id);
        var key1 = gateway.CreateCalls[0].IdempotencyKey;
        Assert.Equal($"tuition:{enrollment.Id}:none:0:v2", key1);

        enrollment.ExtendPaidUntil(Now, 30);
        await context.SaveChangesAsync();

        clock.UtcNow = Now.AddMinutes(35);
        await service.CreateIntentAsync(enrollment.Id);

        var key2 = gateway.CreateCalls[1].IdempotencyKey;
        Assert.Equal($"tuition:{enrollment.Id}:{enrollment.PaidUntil:O}:1:v2", key2);
        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public async Task GetLatestAsync_ReturnsLatest_AndThrowsWhenNone()
    {
        var (context, student, _, enrollment) = await SetupScenarioAsync(price: 100m);
        var service = CreateService(context, student);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetLatestAsync(enrollment.Id));

        var payment = new CoursePayment(enrollment.Id, "pi_1", 10000, "bam", PaymentStatus.RequiresAction);
        context.Set<CoursePayment>().Add(payment);
        await context.SaveChangesAsync();

        var latest = await service.GetLatestAsync(enrollment.Id);
        Assert.Equal("pi_1", latest.PaymentIntentId);
        Assert.Equal(10000, latest.AmountCents);
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsPaymentsNewestFirst()
    {
        var (context, student, _, enrollment) = await SetupScenarioAsync(price: 100m);
        var clock = new MutableClock(Now);
        var service = CreateService(context, student, clock: clock);

        var p1 = new CoursePayment(enrollment.Id, "pi_1", 10000, "bam", PaymentStatus.Succeeded);
        p1.CreatedAt = Now.AddDays(-10);
        var p2 = new CoursePayment(enrollment.Id, "pi_2", 10000, "bam", PaymentStatus.RequiresAction);
        p2.CreatedAt = Now;
        context.Set<CoursePayment>().AddRange(p1, p2);
        await context.SaveChangesAsync();

        var history = await service.GetHistoryAsync(enrollment.Id);
        Assert.Equal(2, history.Count);
        Assert.Equal("pi_2", history[0].PaymentIntentId);
        Assert.Equal("pi_1", history[1].PaymentIntentId);
    }

    [Fact]
    public async Task CreateIntentAsync_WhenGatewayThrows_Throws_PaymentProviderUnavailable()
    {
        var (context, student, _, enrollment) = await SetupScenarioAsync(price: 100m);
        var service = CreateService(context, student, new ThrowingPaymentGateway());

        var ex = await Assert.ThrowsAsync<PaymentProviderUnavailableException>(() => service.CreateIntentAsync(enrollment.Id));
        Assert.Equal(Messages.PaymentProviderUnavailable, ex.Message);
    }

    private static Task<(ENoteContext Context, Student Student, Course Course, Enrollment Enrollment)> SetupScenarioAsync(decimal price) =>
        TuitionTestData.SetupScenarioAsync(Now, price);

    private static TuitionPaymentService CreateService(
        IAppDbContext context,
        Student student,
        IPaymentGateway? gateway = null,
        IClock? clock = null)
    {
        var actor = new StubCurrentActor(student: student);
        return new(
            context,
            TestMapper.Create(),
            clock ?? new FixedClock(Now),
            actor,
            gateway ?? new FakePaymentGateway(),
            new StripeOptions { Currency = "bam", StatementDescriptor = "ENOTE" },
            NullLogger<TuitionPaymentService>.Instance);
    }

    private sealed class MutableClock(DateTime initial) : IClock
    {
        public DateTime UtcNow { get; set; } = initial;
    }
}
