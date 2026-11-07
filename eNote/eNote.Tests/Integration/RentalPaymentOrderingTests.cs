using eNote.Application.Common.Persistence;
using eNote.Application.Features.Payments;
using eNote.Application.Features.Rentals.Payments.Services;
using eNote.Infrastructure.Identity;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace eNote.Tests.Integration;

[Collection(SqlServerCollection.Name)]
public sealed class RentalPaymentOrderingTests(SqlServerFixture fixture)
{
    private static readonly DateTime Now = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task PaymentStatusForStore_AfterDuplicateRefunded_ReturnsTheSettledPayment()
    {
        var database = await fixture.CreateDatabaseAsync();
        await using var context = database.CreateContext();

        var (student, instrument) = await SeedRentalStudentAndInstrumentAsync(context);
        var rental = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now, null);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();

        var original = new RentalPayment(rental.Id, rental.MusicStoreId, "pi_original", 5000, "eur", PaymentStatus.RequiresAction);
        original.MarkSucceeded("ch_original", "evt_original", Now);
        context.Set<RentalPayment>().Add(original);
        await context.SaveChangesAsync();

        var duplicate = new RentalPayment(rental.Id, rental.MusicStoreId, "pi_duplicate", 5000, "eur", PaymentStatus.RequiresAction);
        context.Set<RentalPayment>().Add(duplicate);
        await context.SaveChangesAsync();
        duplicate.MarkSucceeded("ch_duplicate", "evt_duplicate", Now);
        duplicate.ApplyRefund(5000, "re_duplicate", Now);
        duplicate.CreatedAt = Now.AddMinutes(5);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var dto = await service.GetPaymentStatusForStoreAsync(rental.Id);

        Assert.Equal("pi_original", dto.PaymentIntentId);
        Assert.Equal(PaymentStatus.Succeeded, dto.Status);
        Assert.Null(dto.RefundedCents);
    }

    [Fact]
    public async Task Refund_ChoosesTheNewestSettledPayment_AndCountsItsEarlierRefund()
    {
        var database = await fixture.CreateDatabaseAsync();
        await using var context = database.CreateContext();

        var (student, instrument) = await SeedRentalStudentAndInstrumentAsync(context);
        var rental = RentalTestData.CreateCompletedRental(instrument, student.Id, Now);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();

        var older = new RentalPayment(rental.Id, rental.MusicStoreId, "pi_older", 5000, "eur", PaymentStatus.RequiresAction);
        older.MarkSucceeded("ch_older", "evt_older", Now);
        context.Set<RentalPayment>().Add(older);
        await context.SaveChangesAsync();

        var newer = new RentalPayment(rental.Id, rental.MusicStoreId, "pi_newer", 5000, "eur", PaymentStatus.RequiresAction);
        context.Set<RentalPayment>().Add(newer);
        await context.SaveChangesAsync();
        newer.MarkSucceeded("ch_newer", "evt_newer", Now);
        newer.ApplyRefund(2000, "re_newer", Now);
        newer.CreatedAt = Now.AddMinutes(5);
        await context.SaveChangesAsync();

        await using var freshContext = database.CreateContext();
        var gateway = new FakePaymentGateway();
        var service = CreateService(freshContext, gateway);

        var dto = await service.RefundAsync(rental.Id, 1500);

        var call = Assert.Single(gateway.RefundCalls);
        Assert.Equal("pi_newer", call.PaymentIntentId);
        Assert.Equal(PaymentStatus.PartiallyRefunded, dto.Status);
        Assert.Equal(3500, dto.RefundedCents);
    }

    private static RentalPaymentService CreateService(IAppDbContext context, FakePaymentGateway? gateway = null)
    {
        var actor = new StubCurrentActor(storeId: 1);

        return new(
            context,
            TestMapper.Create(),
            new FixedClock(Now),
            actor,
            actor,
            gateway ?? new FakePaymentGateway(),
            new RecordingNotificationDispatcher(),
            new PaymentOptions { Currency = "eur", RentalStatementDescriptorSuffix = "ENOTE RENT T", TuitionStatementDescriptorSuffix = "ENOTE TUIT T" },
            NullLogger<RentalPaymentService>.Instance);
    }

    private static async Task<(Student Student, Instrument Instrument)> SeedRentalStudentAndInstrumentAsync(ENoteContext context)
    {
        var user = new AppUser
        {
            UserName = "renter",
            NormalizedUserName = "RENTER",
            Email = "renter@example.com",
            NormalizedEmail = "RENTER@EXAMPLE.COM",
            IsActive = true,
            FirstName = "Test",
            LastName = "User"
        };
        context.Set<AppUser>().Add(user);
        await context.SaveChangesAsync();

        var student = new Student(user.Id, Now.AddMonths(-1));
        context.Set<Student>().Add(student);

        var store = new MusicStore("Music Store", "09:00-17:00");
        context.Set<MusicStore>().Add(store);
        await context.SaveChangesAsync();

        var type = new InstrumentType { Type = "Guitar", MonthlyFee = 50m };
        context.Set<InstrumentType>().Add(type);
        await context.SaveChangesAsync();

        var instrument = new Instrument("Model", TestManufacturers.Id("Manufacturer"), null, null, type.Id, store.Id);
        context.Set<Instrument>().Add(instrument);
        await context.SaveChangesAsync();

        return (student, instrument);
    }
}
