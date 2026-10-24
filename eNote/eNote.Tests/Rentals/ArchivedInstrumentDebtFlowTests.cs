using eNote.Application.Features.Rentals.InstrumentRentals;
using eNote.Application.Features.Rentals.InstrumentRentals.Services;
using eNote.Application.Features.Rentals.Payments.Services;
using eNote.Infrastructure.Messaging;
using eNote.Infrastructure.Payments.Stripe;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Stripe;
using Event = Stripe.Event;

namespace eNote.Tests.Rentals;

public sealed class ArchivedInstrumentDebtFlowTests
{
    private static readonly DateTime Now = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CompletedUnpaidRental_AfterArchive_IsPayableAndUnblocksNewRequest()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await SeedStudentAsync(context);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = RentalTestData.CreateCompletedRental(instrument, student.Id, Now);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();

        instrument.SoftDelete();
        await context.SaveChangesAsync();

        var actor = new StubCurrentActor(student: student, storeId: 1);
        var queryService = CreateQueryService(context, actor);

        var debt = await queryService.GetDebtForStudentAsync();
        Assert.True(debt.HasUnpaidDebt);
        Assert.Equal(rental.Id, debt.RentalId);

        var detail = await queryService.GetByIdForStudentAsync(rental.Id);
        Assert.False(detail.InstrumentIsActive);

        var gateway = new FakePaymentGateway();
        var intent = await CreatePaymentService(context, actor, gateway).CreatePaymentIntentAsync(rental.Id);
        Assert.Equal("pi_test_1", intent.PaymentIntentId);

        var webhook = CreateWebhookService(context, gateway);
        await webhook.HandleAsync(CreateSucceededEvent(intent.PaymentIntentId), "{}");

        var paidRental = await context.Set<InstrumentRental>().SingleAsync(x => x.Id == rental.Id);
        Assert.True(paidRental.IsPaid);
        Assert.False((await queryService.GetDebtForStudentAsync()).HasUnpaidDebt);

        var newInstrument = new Instrument("Second Model", "Second Manufacturer", null, null, instrument.InstrumentTypeId, instrument.MusicStoreId);
        context.Set<Instrument>().Add(newInstrument);
        await context.SaveChangesAsync();

        var created = await CreateCommandService(context, actor)
            .CreateRequestAsync(new RentalCreateRequest { InstrumentId = newInstrument.Id });

        Assert.Equal(newInstrument.Id, created.InstrumentId);
        Assert.Equal(InstrumentRentalStatus.Pending, created.RentalStatus);
    }

    private static async Task<Student> SeedStudentAsync(ENoteContext context)
    {
        var student = new Student(appUserId: 100, Now.AddMonths(-1));
        student.UpdateMembership(Now.AddDays(1));
        context.Set<Student>().Add(student);
        await context.SaveChangesAsync();
        return student;
    }

    private static RentalQueryService CreateQueryService(ENoteContext context, StubCurrentActor actor) =>
        new(context, TestMapper.Create(), actor, new FixedClock(Now), new StubDisplayNameService());

    private static RentalPaymentService CreatePaymentService(ENoteContext context, StubCurrentActor actor, FakePaymentGateway gateway) =>
        new(context,
            TestMapper.Create(),
            new FixedClock(Now),
            actor,
            actor,
            gateway,
            new NoOpNotificationDispatcher(),
            new StripeOptions { Currency = "eur" },
            NullLogger<RentalPaymentService>.Instance);

    private static RentalCommandService CreateCommandService(ENoteContext context, StubCurrentActor actor) =>
        new(context,
            TestMapper.Create(),
            new FixedClock(Now),
            actor,
            actor,
            actor,
            new NoOpNotificationDispatcher(),
            new StubDisplayNameService());

    private static StripeWebhookService CreateWebhookService(ENoteContext context, FakePaymentGateway gateway) =>
        new(context,
            new FixedClock(Now),
            new StripeOptions { Currency = "eur" },
            gateway,
            new NoOpNotificationDispatcher(),
            new EnrollmentNotificationDispatcher(context, new FixedClock(Now)),
            NullLogger<StripeWebhookService>.Instance);

    private static Event CreateSucceededEvent(string paymentIntentId) => new()
    {
        Id = "evt_archived_debt_paid",
        Type = "payment_intent.succeeded",
        Data = new Stripe.EventData
        {
            Object = new PaymentIntent
            {
                Id = paymentIntentId,
                Status = "succeeded",
                Amount = 1000,
                Currency = "eur",
                ClientSecret = $"{paymentIntentId}_secret",
                LatestChargeId = "ch_test_1"
            }
        }
    };
}
