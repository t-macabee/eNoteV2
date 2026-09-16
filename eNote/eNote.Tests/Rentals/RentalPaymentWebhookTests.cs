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
    public async Task HandleWebhook_UnknownPaymentIntent_IsNoOp()
    {
        var (context, _, _) = await SeedRequiresActionPaymentAsync();
        var service = CreateWebhookService(context);
        var evt = CreatePaymentIntentEvent("evt_test_unknown", "payment_intent.succeeded", "pi_unknown", "succeeded", "ch_unknown");

        await service.HandleAsync(evt, "{}");

        Assert.Empty(await context.Set<StripeWebhookEvent>().ToListAsync());
        Assert.Equal(PaymentStatus.RequiresAction, (await context.Set<RentalPayment>().SingleAsync()).Status);
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

    private static StripeWebhookService CreateWebhookService(ENoteContext context) =>
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
}
