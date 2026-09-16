using eNote.Application.Features.Rentals.Payments.Services;
using eNote.Domain.Entities.Academic;
using eNote.Domain.Entities.Rentals;
using eNote.Domain.Enums;
using eNote.Infrastructure.Data;
using eNote.Infrastructure.Payments.Stripe;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Stripe;

namespace eNote.Tests.Academic;

public sealed class TuitionPaymentWebhookTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task HandleWebhook_Succeeded_ExtendsPaidUntil_RecordsPeriod_AndMarksSucceeded()
    {
        var (context, enrollment, payment) = await SeedRequiresActionTuitionPaymentAsync();
        var service = CreateWebhookService(context);
        var evt = CreatePaymentIntentEvent("evt_tuition_succeeded_1", "payment_intent.succeeded", payment.StripePaymentIntentId, "succeeded", "ch_tuition_1");

        await service.HandleAsync(evt, "{}");

        var reloadedPayment = await context.Set<CoursePayment>().SingleAsync(p => p.Id == payment.Id);
        var reloadedEnrollment = await context.Set<Enrollment>().SingleAsync(e => e.Id == enrollment.Id);

        Assert.Equal(PaymentStatus.Succeeded, reloadedPayment.Status);
        Assert.Equal("ch_tuition_1", reloadedPayment.StripeChargeId);
        Assert.Equal("evt_tuition_succeeded_1", reloadedPayment.StripeEventId);
        Assert.Equal(Now, reloadedPayment.PaidAt);
        Assert.Equal(Now, reloadedPayment.PeriodStart);
        Assert.Equal(Now.AddDays(30), reloadedPayment.PeriodEnd);
        Assert.Equal(Now.AddDays(30), reloadedEnrollment.PaidUntil);

        Assert.Single(await context.Set<StripeWebhookEvent>().Where(e => e.StripeEventId == "evt_tuition_succeeded_1").ToListAsync());
    }

    [Fact]
    public async Task HandleWebhook_Succeeded_EarlyRenewal_ExtendsFromExistingPaidUntil()
    {
        var (context, enrollment, payment) = await SeedRequiresActionTuitionPaymentAsync();
        enrollment.ExtendPaidUntil(Now, 10);
        await context.SaveChangesAsync();

        var service = CreateWebhookService(context);
        var evt = CreatePaymentIntentEvent("evt_tuition_succeeded_2", "payment_intent.succeeded", payment.StripePaymentIntentId, "succeeded", "ch_tuition_2");

        await service.HandleAsync(evt, "{}");

        var reloadedPayment = await context.Set<CoursePayment>().SingleAsync(p => p.Id == payment.Id);
        var reloadedEnrollment = await context.Set<Enrollment>().SingleAsync(e => e.Id == enrollment.Id);

        Assert.Equal(Now.AddDays(10), reloadedPayment.PeriodStart);
        Assert.Equal(Now.AddDays(40), reloadedPayment.PeriodEnd);
        Assert.Equal(Now.AddDays(40), reloadedEnrollment.PaidUntil);
    }

    [Fact]
    public async Task HandleWebhook_Succeeded_NullChargeId_StillExtendsPaidUntil_LeavesChargeNull()
    {
        var (context, enrollment, payment) = await SeedRequiresActionTuitionPaymentAsync();
        var service = CreateWebhookService(context);
        var evt = CreatePaymentIntentEvent("evt_tuition_succeeded_null_charge", "payment_intent.succeeded", payment.StripePaymentIntentId, "succeeded", chargeId: null);

        await service.HandleAsync(evt, "{}");

        var reloadedPayment = await context.Set<CoursePayment>().SingleAsync(p => p.Id == payment.Id);
        var reloadedEnrollment = await context.Set<Enrollment>().SingleAsync(e => e.Id == enrollment.Id);

        Assert.Equal(Now.AddDays(30), reloadedEnrollment.PaidUntil);
        Assert.Equal(PaymentStatus.Succeeded, reloadedPayment.Status);
        Assert.Null(reloadedPayment.StripeChargeId);
    }

    [Fact]
    public async Task HandleWebhook_Failed_MarksFailed_DoesNotExtendPaidUntil()
    {
        var (context, enrollment, payment) = await SeedRequiresActionTuitionPaymentAsync();
        var service = CreateWebhookService(context);
        var evt = CreatePaymentIntentEvent("evt_tuition_failed_1", "payment_intent.payment_failed", payment.StripePaymentIntentId, "requires_payment_method", null);

        await service.HandleAsync(evt, "{}");

        var reloadedPayment = await context.Set<CoursePayment>().SingleAsync(p => p.Id == payment.Id);
        var reloadedEnrollment = await context.Set<Enrollment>().SingleAsync(e => e.Id == enrollment.Id);

        Assert.Equal(PaymentStatus.Failed, reloadedPayment.Status);
        Assert.Equal("evt_tuition_failed_1", reloadedPayment.StripeEventId);
        Assert.Null(reloadedEnrollment.PaidUntil);
        Assert.Single(await context.Set<StripeWebhookEvent>().Where(e => e.StripeEventId == "evt_tuition_failed_1").ToListAsync());
    }

    [Fact]
    public async Task HandleWebhook_Replay_NoOps()
    {
        var (context, enrollment, payment) = await SeedRequiresActionTuitionPaymentAsync();
        var service = CreateWebhookService(context);
        var evt = CreatePaymentIntentEvent("evt_tuition_replay_1", "payment_intent.succeeded", payment.StripePaymentIntentId, "succeeded", "ch_tuition_1");

        await service.HandleAsync(evt, "{}");
        await service.HandleAsync(evt, "{}");

        var reloadedEnrollment = await context.Set<Enrollment>().SingleAsync(e => e.Id == enrollment.Id);
        Assert.Equal(Now.AddDays(30), reloadedEnrollment.PaidUntil);
        Assert.Single(await context.Set<StripeWebhookEvent>().Where(e => e.StripeEventId == "evt_tuition_replay_1").ToListAsync());
    }

    [Fact]
    public async Task HandleWebhook_ChargeRefunded_LeavesCoursePaymentAndEnrollmentUntouched_AndSavesEventRow()
    {
        var (context, enrollment, payment) = await SeedRequiresActionTuitionPaymentAsync();
        var service = CreateWebhookService(context);
        var successEvt = CreatePaymentIntentEvent("evt_tuition_succeeded_ref", "payment_intent.succeeded", payment.StripePaymentIntentId, "succeeded", "ch_tuition_ref");
        await service.HandleAsync(successEvt, "{}");

        var refundEvt = CreateChargeRefundedEvent("evt_tuition_refunded_1", payment.StripePaymentIntentId, "ch_tuition_ref", payment.AmountChargedCents);
        await service.HandleAsync(refundEvt, "{}");

        var reloadedPayment = await context.Set<CoursePayment>().SingleAsync(p => p.Id == payment.Id);
        var reloadedEnrollment = await context.Set<Enrollment>().SingleAsync(e => e.Id == enrollment.Id);

        Assert.Equal(PaymentStatus.Succeeded, reloadedPayment.Status);
        Assert.Equal(Now.AddDays(30), reloadedEnrollment.PaidUntil);
        Assert.Single(await context.Set<StripeWebhookEvent>().Where(e => e.StripeEventId == "evt_tuition_refunded_1").ToListAsync());
    }

    [Fact]
    public async Task HandleWebhook_Succeeded_AppliesPayment_WhenCourseDeactivated()
    {
        var (context, enrollment, payment) = await SeedRequiresActionTuitionPaymentAsync();
        var course = await context.Set<Course>().SingleAsync(c => c.Id == enrollment.CourseId);
        course.SoftDelete();
        await context.SaveChangesAsync();
        var service = CreateWebhookService(context);
        var evt = CreatePaymentIntentEvent("evt_tuition_succeeded_deactivated", "payment_intent.succeeded", payment.StripePaymentIntentId, "succeeded", "ch_tuition_deactivated");

        await service.HandleAsync(evt, "{}");

        var reloadedPayment = await context.Set<CoursePayment>().IgnoreQueryFilters().SingleAsync(p => p.Id == payment.Id);
        var reloadedEnrollment = await context.Set<Enrollment>().IgnoreQueryFilters().SingleAsync(e => e.Id == enrollment.Id);
        Assert.Equal(PaymentStatus.Succeeded, reloadedPayment.Status);
        Assert.Equal(Now.AddDays(30), reloadedEnrollment.PaidUntil);
        Assert.Single(await context.Set<StripeWebhookEvent>().Where(e => e.StripeEventId == "evt_tuition_succeeded_deactivated").ToListAsync());
    }

    [Fact]
    public async Task HandleWebhook_Succeeded_CanceledEnrollment_DoesNotExtendPaidUntil()
    {
        var (context, enrollment, payment) = await SeedRequiresActionTuitionPaymentAsync();
        enrollment.UpdateStatus(EnrollmentStatus.Canceled);
        await context.SaveChangesAsync();
        var service = CreateWebhookService(context);
        var evt = CreatePaymentIntentEvent("evt_tuition_succeeded_canceled", "payment_intent.succeeded", payment.StripePaymentIntentId, "succeeded", "ch_tuition_canceled");

        await service.HandleAsync(evt, "{}");

        var reloadedPayment = await context.Set<CoursePayment>().SingleAsync(p => p.Id == payment.Id);
        var reloadedEnrollment = await context.Set<Enrollment>().SingleAsync(e => e.Id == enrollment.Id);
        Assert.Equal(PaymentStatus.Succeeded, reloadedPayment.Status);
        Assert.Null(reloadedEnrollment.PaidUntil);
        Assert.Single(await context.Set<StripeWebhookEvent>().Where(e => e.StripeEventId == "evt_tuition_succeeded_canceled").ToListAsync());
    }

    [Fact]
    public async Task HandleWebhook_RentalIntent_StillRoutesToRentalPayment()
    {
        var (context, enrollment, _) = await SeedRequiresActionTuitionPaymentAsync();
        var student = await context.Set<Student>().FirstAsync();
        var type = new InstrumentType { Type = "Guitar", MonthlyFee = 50m };
        context.Set<InstrumentType>().Add(type);
        await context.SaveChangesAsync();

        var store = new MusicStore("Music Shop", "09-17");
        context.Set<MusicStore>().Add(store);
        await context.SaveChangesAsync();

        var instrument = new Instrument("Stradivarius", "Yamaha", null, null, type.Id, store.Id);
        context.Set<Instrument>().Add(instrument);
        await context.SaveChangesAsync();

        var rental = RentalTestData.CreateCompletedRental(instrument, student.Id, Now);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();

        var rentalPayment = new RentalPayment(rental.Id, store.Id, "pi_rental_1", 5000, "bam", PaymentStatus.RequiresAction);
        context.Set<RentalPayment>().Add(rentalPayment);
        await context.SaveChangesAsync();

        var service = CreateWebhookService(context);
        var evt = CreatePaymentIntentEvent("evt_rental_succeeded_1", "payment_intent.succeeded", "pi_rental_1", "succeeded", "ch_rental_1");

        await service.HandleAsync(evt, "{}");

        var reloadedRentalPayment = await context.Set<RentalPayment>().SingleAsync(p => p.Id == rentalPayment.Id);
        var reloadedRental = await context.Set<InstrumentRental>().SingleAsync(r => r.Id == rental.Id);
        var reloadedEnrollment = await context.Set<Enrollment>().SingleAsync(e => e.Id == enrollment.Id);

        Assert.Equal(PaymentStatus.Succeeded, reloadedRentalPayment.Status);
        Assert.True(reloadedRental.IsPaid);
        Assert.Null(reloadedEnrollment.PaidUntil);
    }

    private static async Task<(ENoteContext Context, Enrollment Enrollment, CoursePayment Payment)> SeedRequiresActionTuitionPaymentAsync()
    {
        var options = new DbContextOptionsBuilder<ENoteContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var student = new Student(50, Now);
        var instructor = new Instructor(100);
        var context = new ENoteContext(options, new FixedClock(Now), new StubCurrentActor(student: student)) { ExplicitStoreId = 1 };
        context.Set<Student>().Add(student);
        context.Set<Instructor>().Add(instructor);
        await context.SaveChangesAsync();

        var course = new Course("Guitar", null, 100m, Now, Now.AddMonths(6), instructor.Id);
        course.SetPublishedStatus(true);
        context.Set<Course>().Add(course);
        await context.SaveChangesAsync();

        var enrollment = new Enrollment(student.Id, course.Id, EnrollmentStatus.Active);
        context.Set<Enrollment>().Add(enrollment);
        await context.SaveChangesAsync();

        var payment = new CoursePayment(enrollment.Id, "pi_tuition_test_1", 10000, "bam", PaymentStatus.RequiresAction);
        context.Set<CoursePayment>().Add(payment);
        await context.SaveChangesAsync();

        return (context, enrollment, payment);
    }

    private static StripeWebhookService CreateWebhookService(ENoteContext context) =>
        new(context, new FixedClock(Now), new StripeOptions { Currency = "bam", WebhookSecret = "whsec_test" }, NullLogger<StripeWebhookService>.Instance);

    private static Event CreatePaymentIntentEvent(string eventId, string type, string paymentIntentId, string status, string? chargeId)
    {
        var intent = new PaymentIntent
        {
            Id = paymentIntentId,
            Status = status,
            Amount = 10000,
            Currency = "bam",
            ClientSecret = $"{paymentIntentId}_secret",
            LatestChargeId = chargeId
        };

        return new Event
        {
            Id = eventId,
            Type = type,
            Data = new Stripe.EventData { Object = intent }
        };
    }

    private static Event CreateChargeRefundedEvent(string eventId, string paymentIntentId, string chargeId, long amountRefunded)
    {
        var refunds = new StripeList<Refund>
        {
            Data = [new Refund { Id = "re_tuition_1", Amount = amountRefunded }]
        };

        var charge = new Charge
        {
            Id = chargeId,
            PaymentIntentId = paymentIntentId,
            AmountRefunded = amountRefunded,
            Refunds = refunds
        };

        return new Event
        {
            Id = eventId,
            Type = "charge.refunded",
            Data = new Stripe.EventData { Object = charge }
        };
    }
}
