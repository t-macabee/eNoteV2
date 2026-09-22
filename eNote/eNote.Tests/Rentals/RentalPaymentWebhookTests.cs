using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Persistence;
using eNote.Application.Constants;
using eNote.Application.Features.Rentals.Payments.Services;
using eNote.Infrastructure.Payments.Stripe;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Stripe;

namespace eNote.Tests.Rentals;

/// <summary>
/// StripeWebhookService tests. Per the plan, these bypass Stripe's signature
/// verification (EventUtility.ConstructEvent is Stripe.net's own code) and call
/// the handler directly with a pre-parsed Event object.
/// </summary>
public sealed class RentalPaymentWebhookTests
{
    private static readonly DateTime Now = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task HandleWebhook_Succeeded_MarksPaidAndIdempotent()
    {
        var (context, rental, _) = await SeedRequiresActionPaymentAsync();
        var service = CreateWebhookService(context);
        var evt = CreatePaymentIntentEvent("evt_test_succeeded_1", "payment_intent.succeeded", "pi_test_1", "succeeded", "ch_test_1");

        await service.HandleAsync(evt, "{}");
        await service.HandleAsync(evt, "{}");

        var payment = await context.Set<RentalPayment>().SingleAsync();
        var reloadedRental = await context.Set<InstrumentRental>().SingleAsync(x => x.Id == rental.Id);
        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        Assert.Equal("ch_test_1", payment.StripeChargeId);
        Assert.Equal("evt_test_succeeded_1", payment.StripeEventId);
        Assert.True(reloadedRental.IsPaid);
        Assert.Equal(5000L, payment.AmountChargedCents);
        Assert.Single(await context.Set<StripeWebhookEvent>().ToListAsync());
    }

    [Fact]
    public async Task HandleWebhook_Failed_DoesNotMarkPaid()
    {
        var (context, rental, _) = await SeedRequiresActionPaymentAsync();
        var service = CreateWebhookService(context);
        var evt = CreatePaymentIntentEvent("evt_test_failed_1", "payment_intent.payment_failed", "pi_test_1", "requires_payment_method", null);

        await service.HandleAsync(evt, "{}");

        var payment = await context.Set<RentalPayment>().SingleAsync();
        var reloadedRental = await context.Set<InstrumentRental>().SingleAsync(x => x.Id == rental.Id);
        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.False(reloadedRental.IsPaid);
        Assert.Single(await context.Set<StripeWebhookEvent>().ToListAsync());
    }

    [Fact]
    public async Task HandleWebhook_ChargeRefunded_AppliesRefundAndKeepsIsPaid()
    {
        var (context, rental, _) = await SeedSucceededPaymentAsync();
        var service = CreateWebhookService(context);
        var evt = CreateChargeRefundedEvent("evt_test_refunded_1", "pi_test_1", "ch_test_1", 5000);

        await service.HandleAsync(evt, "{}");

        var payment = await context.Set<RentalPayment>().SingleAsync();
        var reloadedRental = await context.Set<InstrumentRental>().SingleAsync(x => x.Id == rental.Id);
        Assert.Equal(PaymentStatus.Refunded, payment.Status);
        Assert.Equal(5000, payment.RefundedCents);
        Assert.NotNull(payment.StripeRefundId);
        Assert.True(reloadedRental.IsPaid);
    }

    [Fact]
    public async Task HandleWebhook_SucceededWithoutChargeId_MarksPaidWithNullChargeId()
    {
        var (context, rental, _) = await SeedRequiresActionPaymentAsync();
        var service = CreateWebhookService(context);
        var evt = CreatePaymentIntentEvent("evt_test_succeeded_nocharge", "payment_intent.succeeded", "pi_test_1", "succeeded", null);

        await service.HandleAsync(evt, "{}");

        var payment = await context.Set<RentalPayment>().SingleAsync();
        var reloadedRental = await context.Set<InstrumentRental>().SingleAsync(x => x.Id == rental.Id);
        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        Assert.Null(payment.StripeChargeId);
        Assert.True(reloadedRental.IsPaid);
    }

    [Fact]
    public async Task HandleWebhook_ChargeRefundedWithoutRefundList_RefundsWithNullRefundId()
    {
        var (context, rental, _) = await SeedSucceededPaymentAsync();
        var service = CreateWebhookService(context);
        var evt = CreateChargeRefundedEvent("evt_test_refunded_norefundid", "pi_test_1", "ch_test_1", 5000, includeRefunds: false);

        await service.HandleAsync(evt, "{}");

        var payment = await context.Set<RentalPayment>().SingleAsync();
        var reloadedRental = await context.Set<InstrumentRental>().SingleAsync(x => x.Id == rental.Id);
        Assert.Equal(PaymentStatus.Refunded, payment.Status);
        Assert.Null(payment.StripeRefundId);
        Assert.True(reloadedRental.IsPaid);
    }

    [Fact]
    public async Task HandleWebhook_UnknownPaymentIntent_ThrowsForStripeRetry()
    {
        var (context, _, _) = await SeedRequiresActionPaymentAsync();
        var service = CreateWebhookService(context);
        var evt = CreatePaymentIntentEvent("evt_test_unknown", "payment_intent.succeeded", "pi_unknown", "succeeded", "ch_unknown");

        var exception = await Assert.ThrowsAsync<PaymentNotYetVisibleException>(() => service.HandleAsync(evt, "{}"));

        Assert.Equal(503, exception.StatusCode);
        Assert.Empty(await context.Set<StripeWebhookEvent>().ToListAsync());
        Assert.Equal(PaymentStatus.RequiresAction, (await context.Set<RentalPayment>().SingleAsync()).Status);
    }

    [Fact]
    public async Task HandleWebhook_UnknownPaymentIntent_Failed_ThrowsForStripeRetry()
    {
        var (context, _, _) = await SeedRequiresActionPaymentAsync();
        var service = CreateWebhookService(context);
        var evt = CreatePaymentIntentEvent("evt_test_unknown_failed", "payment_intent.payment_failed", "pi_unknown", "requires_payment_method", null);

        var exception = await Assert.ThrowsAsync<PaymentNotYetVisibleException>(() => service.HandleAsync(evt, "{}"));

        Assert.Equal(503, exception.StatusCode);
        Assert.Empty(await context.Set<StripeWebhookEvent>().ToListAsync());
        Assert.Equal(PaymentStatus.RequiresAction, (await context.Set<RentalPayment>().SingleAsync()).Status);
    }

    [Fact]
    public async Task HandleWebhook_UnknownPaymentIntent_Refunded_ThrowsForStripeRetry()
    {
        var (context, _, _) = await SeedSucceededPaymentAsync();
        var service = CreateWebhookService(context);
        var evt = CreateChargeRefundedEvent("evt_test_unknown_refunded", "pi_unknown", "ch_unknown", 5000);

        var exception = await Assert.ThrowsAsync<PaymentNotYetVisibleException>(() => service.HandleAsync(evt, "{}"));

        Assert.Equal(503, exception.StatusCode);
        Assert.Empty(await context.Set<StripeWebhookEvent>().ToListAsync());
        Assert.Equal(PaymentStatus.Succeeded, (await context.Set<RentalPayment>().SingleAsync()).Status);
    }

    [Fact]
    public async Task HandleWebhook_UnknownPaymentIntent_RefundUpdated_ThrowsForStripeRetry()
    {
        var (context, _, _) = await SeedSucceededPaymentAsync();
        var service = CreateWebhookService(context);
        var evt = CreateRefundUpdatedEvent("evt_test_unknown_refund_updated", "pi_unknown", "re_unknown", 2000, "succeeded");

        var exception = await Assert.ThrowsAsync<PaymentNotYetVisibleException>(() => service.HandleAsync(evt, "{}"));

        Assert.Equal(503, exception.StatusCode);
        Assert.Empty(await context.Set<StripeWebhookEvent>().ToListAsync());
        Assert.Equal(PaymentStatus.Succeeded, (await context.Set<RentalPayment>().SingleAsync()).Status);
    }

    [Fact]
    public async Task HandleWebhook_Succeeded_AppliesPayment_WhenInstrumentDeactivated()
    {
        var (context, rental, _) = await SeedRequiresActionPaymentAsync();
        var instrument = await context.Set<Instrument>().SingleAsync(x => x.Id == rental.InstrumentId);
        instrument.SoftDelete();
        await context.SaveChangesAsync();
        var service = CreateWebhookService(context);
        var evt = CreatePaymentIntentEvent("evt_test_succeeded_deactivated", "payment_intent.succeeded", "pi_test_1", "succeeded", "ch_test_1");

        await service.HandleAsync(evt, "{}");

        var payment = await context.Set<RentalPayment>().IgnoreQueryFilters().SingleAsync();
        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        var reloadedRental = await context.Set<InstrumentRental>().IgnoreQueryFilters().SingleAsync(x => x.Id == rental.Id);
        Assert.True(reloadedRental.IsPaid);
        Assert.Single(await context.Set<StripeWebhookEvent>().ToListAsync());
    }

    [Fact]
    public async Task HandleWebhook_ChargeRefunded_SecondRefund_Accumulates()
    {
        var (context, rental, _) = await SeedSucceededPaymentAsync();
        var service = CreateWebhookService(context);

        await service.HandleAsync(CreateChargeRefundedEvent("evt_test_refunded_first", "pi_test_1", "ch_test_1", 2000), "{}");
        await service.HandleAsync(CreateChargeRefundedEvent("evt_test_refunded_second", "pi_test_1", "ch_test_1", 3500), "{}");

        var payment = await context.Set<RentalPayment>().SingleAsync();
        Assert.Equal(PaymentStatus.PartiallyRefunded, payment.Status);
        Assert.Equal(3500, payment.RefundedCents);
        Assert.Equal(2, await context.Set<StripeWebhookEvent>().CountAsync());
    }

    [Fact]
    public async Task HandleWebhook_ChargeRefundUpdated_Succeeded_AppliesRefundOnce()
    {
        var (context, rental, _) = await SeedSucceededPaymentAsync();
        var service = CreateWebhookService(context);

        await service.HandleAsync(CreateRefundUpdatedEvent("evt_test_refund_updated_1", "pi_test_1", "re_test_1", 2000, "succeeded"), "{}");
        await service.HandleAsync(CreateRefundUpdatedEvent("evt_test_refund_updated_2", "pi_test_1", "re_test_1", 2000, "succeeded"), "{}");

        var payment = await context.Set<RentalPayment>().SingleAsync();
        Assert.Equal(PaymentStatus.PartiallyRefunded, payment.Status);
        Assert.Equal(2000, payment.RefundedCents);
        Assert.Equal("re_test_1", payment.StripeRefundId);
        Assert.Equal(2, await context.Set<StripeWebhookEvent>().CountAsync());
        Assert.True((await context.Set<InstrumentRental>().SingleAsync(x => x.Id == rental.Id)).IsPaid);
    }

    [Fact]
    public async Task HandleWebhook_RefundUpdatedAndChargeRefunded_CountRefundOnce()
    {
        var (context, _, _) = await SeedSucceededPaymentAsync();
        var service = CreateWebhookService(context);

        await service.HandleAsync(CreateRefundUpdatedEvent("evt_test_refund_updated_1", "pi_test_1", "re_test_1", 2000, "succeeded"), "{}");
        await service.HandleAsync(CreateChargeRefundedEvent("evt_test_refunded_1", "pi_test_1", "ch_test_1", 2000), "{}");

        var payment = await context.Set<RentalPayment>().SingleAsync();
        Assert.Equal(PaymentStatus.PartiallyRefunded, payment.Status);
        Assert.Equal(2000, payment.RefundedCents);
    }

    [Fact]
    public async Task HandleWebhook_ChargeRefundUpdated_Failed_DoesNotApplyRefund()
    {
        var (context, _, _) = await SeedSucceededPaymentAsync();
        var service = CreateWebhookService(context);

        await service.HandleAsync(CreateRefundUpdatedEvent("evt_test_refund_updated_failed", "pi_test_1", "re_test_1", 2000, "failed"), "{}");

        var payment = await context.Set<RentalPayment>().SingleAsync();
        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        Assert.Null(payment.RefundedCents);
        Assert.Single(await context.Set<StripeWebhookEvent>().ToListAsync());
    }

    [Fact]
    public async Task HandleWebhook_ChargeRefundedWithoutRefundList_ThenRefundUpdated_CountsOnce()
    {
        var (context, _, _) = await SeedSucceededPaymentAsync();
        var service = CreateWebhookService(context);

        await service.HandleAsync(CreateChargeRefundedEvent("evt_test_refunded_no_list", "pi_test_1", "ch_test_1", 2000, includeRefunds: false), "{}");
        await service.HandleAsync(CreateRefundUpdatedEvent("evt_test_refund_updated_after", "pi_test_1", "re_test_1", 2000, "succeeded"), "{}");

        var payment = await context.Set<RentalPayment>().SingleAsync();
        Assert.Equal(PaymentStatus.PartiallyRefunded, payment.Status);
        Assert.Equal(2000, payment.RefundedCents);
        Assert.Equal(2, await context.Set<StripeWebhookEvent>().CountAsync());
    }

    [Fact]
    public async Task HandleWebhook_ChargeRefundUpdated_OnRequiresActionPayment_DoesNotApply()
    {
        var (context, _, _) = await SeedRequiresActionPaymentAsync();
        var service = CreateWebhookService(context);

        await service.HandleAsync(CreateRefundUpdatedEvent("evt_test_refund_updated_requires_action", "pi_test_1", "re_test_1", 2000, "succeeded"), "{}");

        var payment = await context.Set<RentalPayment>().SingleAsync();
        Assert.Equal(PaymentStatus.RequiresAction, payment.Status);
        Assert.Null(payment.RefundedCents);
        Assert.Single(await context.Set<StripeWebhookEvent>().ToListAsync());
    }

    [Fact]
    public async Task HandleWebhook_ChargeRefundUpdated_Failed_ReversesCountedRefund()
    {
        var (context, _, _) = await SeedSucceededPaymentAsync();
        var service = CreateWebhookService(context);

        await service.HandleAsync(CreateChargeRefundedEvent("evt_test_refunded_pending", "pi_test_1", "ch_test_1", 2000, includeRefunds: false), "{}");
        await service.HandleAsync(CreateRefundUpdatedEvent("evt_test_refund_updated_failed_counted", "pi_test_1", "re_test_1", 2000, "failed"), "{}");

        var payment = await context.Set<RentalPayment>().SingleAsync();
        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        Assert.Null(payment.RefundedCents);
        Assert.Null(payment.StripeRefundId);
    }

    [Fact]
    public async Task HandleWebhook_ChargeRefundUpdated_Failed_OnFullyRefundedPayment_ReversesToSucceeded()
    {
        var (context, _, _) = await SeedSucceededPaymentAsync();
        var service = CreateWebhookService(context);

        await service.HandleAsync(CreateChargeRefundedEvent("evt_test_refunded_full_pending", "pi_test_1", "ch_test_1", 5000, includeRefunds: false), "{}");
        Assert.Equal(PaymentStatus.Refunded, (await context.Set<RentalPayment>().SingleAsync()).Status);

        await service.HandleAsync(CreateRefundUpdatedEvent("evt_test_refund_updated_failed_full", "pi_test_1", "re_test_1", 5000, "failed"), "{}");

        var payment = await context.Set<RentalPayment>().SingleAsync();
        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        Assert.Null(payment.RefundedCents);
        Assert.Null(payment.StripeRefundId);
    }

    [Fact]
    public async Task HandleWebhook_ChargeRefundUpdated_Failed_PartialReversal_LeavesPartiallyRefunded()
    {
        var (context, _, _) = await SeedSucceededPaymentAsync();
        var service = CreateWebhookService(context);

        await service.HandleAsync(CreateChargeRefundedEvent("evt_test_refunded_total_first", "pi_test_1", "ch_test_1", 2000, includeRefunds: false), "{}");
        await service.HandleAsync(CreateChargeRefundedEvent("evt_test_refunded_total_second", "pi_test_1", "ch_test_1", 3500, includeRefunds: false), "{}");
        await service.HandleAsync(CreateRefundUpdatedEvent("evt_test_refund_updated_failed_partial", "pi_test_1", "re_test_1", 1500, "failed"), "{}");

        var payment = await context.Set<RentalPayment>().SingleAsync();
        Assert.Equal(PaymentStatus.PartiallyRefunded, payment.Status);
        Assert.Equal(2000, payment.RefundedCents);
    }

    [Fact]
    public async Task HandleWebhook_ChargeRefundUpdated_ForCoursePayment_RecordsEventOnly()
    {
        var (context, _, _) = await SeedBaseAsync();
        var coursePayment = new CoursePayment(1, "pi_course_test_1", 10000, "eur", PaymentStatus.Succeeded);
        context.Set<CoursePayment>().Add(coursePayment);
        await context.SaveChangesAsync();
        var service = CreateWebhookService(context);

        await service.HandleAsync(CreateRefundUpdatedEvent("evt_test_refund_updated_course", "pi_course_test_1", "re_test_1", 2000, "succeeded"), "{}");

        var reloaded = await context.Set<CoursePayment>().SingleAsync(p => p.Id == coursePayment.Id);
        Assert.Equal(PaymentStatus.Succeeded, reloaded.Status);
        Assert.Null(reloaded.StripeEventId);
        Assert.Single(await context.Set<StripeWebhookEvent>().ToListAsync());
    }

    [Fact]
    public async Task HandleWebhook_CrossEventInterleaving_DoesNotDoubleCountRefund()
    {
        var (context, _, _) = await SeedSucceededPaymentAsync();
        var service = CreateWebhookService(context);

        var chargeEvt = new Event
        {
            Id = "evt_charge_refunded_multi",
            Type = "charge.refunded",
            Data = new Stripe.EventData
            {
                Object = new Charge
                {
                    Id = "ch_test_1",
                    PaymentIntentId = "pi_test_1",
                    AmountRefunded = 3500,
                    Refunds = new StripeList<Refund>
                    {
                        Data =
                        [
                            new Refund { Id = "re_A", Amount = 2000 },
                            new Refund { Id = "re_B", Amount = 1500 }
                        ]
                    }
                }
            }
        };

        await service.HandleAsync(chargeEvt, "{}");

        var payment = await context.Set<RentalPayment>().Include(p => p.Refunds).SingleAsync();
        Assert.Equal(3500, payment.RefundedCents);
        Assert.Equal(2, payment.Refunds.Count);

        var refundBEvt = CreateRefundUpdatedEvent("evt_refund_b_updated", "pi_test_1", "re_B", 1500, "succeeded");
        await service.HandleAsync(refundBEvt, "{}");

        payment = await context.Set<RentalPayment>().Include(p => p.Refunds).SingleAsync();
        Assert.Equal(PaymentStatus.PartiallyRefunded, payment.Status);
        Assert.Equal(3500, payment.RefundedCents);
        Assert.Equal(2, payment.Refunds.Count);
    }

    [Fact]
    public async Task HandleWebhook_DuplicateEvent_DbUniqueViolation_IsIgnoredGracefully()
    {
        var (context, _, _) = await SeedRequiresActionPaymentAsync();
        var inner = new Exception($"duplicate key value violates unique constraint \"{DbConstraintNames.StripeWebhookEventStripeEventIdUniqueIndex}\"");
        var throwingContext = new ThrowingSaveDbContext(context, new DbUpdateException("Unique constraint violated.", inner));
        var service = CreateWebhookService(throwingContext);
        var evt = CreatePaymentIntentEvent("evt_test_race_1", "payment_intent.succeeded", "pi_test_1", "succeeded", "ch_test_1");

        await service.HandleAsync(evt, "{}");
    }

    [Fact]
    public async Task HandleWebhook_ConcurrencyConflict_Rethrows()
    {
        var (context, _, _) = await SeedRequiresActionPaymentAsync();
        var throwingContext = new ThrowingSaveDbContext(context, new DbUpdateConcurrencyException("Concurrency conflict."));
        var service = CreateWebhookService(throwingContext);
        var evt = CreatePaymentIntentEvent("evt_test_concurrency_1", "payment_intent.succeeded", "pi_test_1", "succeeded", "ch_test_1");

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => service.HandleAsync(evt, "{}"));
    }

    // ---- Helpers ----------------------------------------------------------

    private static async Task<(ENoteContext Context, InstrumentRental Rental, RentalPayment Payment)> SeedRequiresActionPaymentAsync()
    {
        var (context, student, instrument) = await SeedBaseAsync();
        var rental = RentalTestData.CreateCompletedRental(instrument, student.Id, Now);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();

        var payment = new RentalPayment(rental.Id, rental.MusicStoreId, "pi_test_1", 5000, "eur", PaymentStatus.RequiresAction);
        context.Set<RentalPayment>().Add(payment);
        await context.SaveChangesAsync();
        return (context, rental, payment);
    }

    private static async Task<(ENoteContext Context, InstrumentRental Rental, RentalPayment Payment)> SeedSucceededPaymentAsync()
    {
        var (context, student, instrument) = await SeedBaseAsync();
        var rental = RentalTestData.CreateCompletedRental(instrument, student.Id, Now);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();

        rental.MarkPaid(5000, Now);
        var payment = new RentalPayment(rental.Id, rental.MusicStoreId, "pi_test_1", 5000, "eur", PaymentStatus.RequiresAction);
        payment.MarkSucceeded("ch_test_1", "evt_seed", Now);
        context.Set<RentalPayment>().Add(payment);
        await context.SaveChangesAsync();
        return (context, rental, payment);
    }

    private static async Task<(ENoteContext Context, Student Student, Instrument Instrument)> SeedBaseAsync()
    {
        var student = new Student(appUserId: 100, enrollmentDate: Now.AddMonths(-1));
        student.UpdateMembership(Now.AddDays(1));
        var currentUser = new StubCurrentActor(student: student);
        var options = new DbContextOptionsBuilder<ENoteContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var context = new ENoteContext(options, new FixedClock(Now), currentUser) { ExplicitStoreId = 1 };

        context.Set<Student>().Add(student);
        await context.SaveChangesAsync();

        var type = new InstrumentType { Type = "Guitar", MonthlyFee = 50m };
        context.Set<InstrumentType>().Add(type);
        await context.SaveChangesAsync();

        var store = new MusicStore("Music Shop", "09-17");
        context.Set<MusicStore>().Add(store);
        await context.SaveChangesAsync();

        var instrument = new Instrument("Stradivarius", "Yamaha", null, null, type.Id, store.Id);
        context.Set<Instrument>().Add(instrument);
        await context.SaveChangesAsync();

        return (context, student, instrument);
    }

    private static StripeWebhookService CreateWebhookService(IAppDbContext context) =>
        new(context, new FixedClock(Now), new StripeOptions { Currency = "eur" }, NullLogger<StripeWebhookService>.Instance);

    private static Event CreatePaymentIntentEvent(string eventId, string type, string paymentIntentId, string status, string? chargeId)
    {
        var intent = new PaymentIntent
        {
            Id = paymentIntentId,
            Status = status,
            Amount = 5000,
            Currency = "eur",
            ClientSecret = "pi_test_1_secret",
            LatestChargeId = chargeId
        };

        return new Event
        {
            Id = eventId,
            Type = type,
            Data = new Stripe.EventData { Object = intent }
        };
    }

    private static Event CreateChargeRefundedEvent(string eventId, string paymentIntentId, string chargeId, long amountRefunded, bool includeRefunds = true)
    {
        var refunds = new StripeList<Refund>();
        if (includeRefunds)
        {
            refunds.Data = [new Refund { Id = "re_test_1", Amount = amountRefunded }];
        }

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

    private static Event CreateRefundUpdatedEvent(string eventId, string paymentIntentId, string refundId, long amount, string status)
    {
        var refund = new Refund
        {
            Id = refundId,
            PaymentIntentId = paymentIntentId,
            Amount = amount,
            Status = status
        };

        return new Event
        {
            Id = eventId,
            Type = "charge.refund.updated",
            Data = new Stripe.EventData { Object = refund }
        };
    }
}
