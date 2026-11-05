using eNote.Application.Features.Rentals.InstrumentRentals;
using eNote.Application.Features.Rentals.InstrumentRentals.Services;
using eNote.Tests.TestUtils;

namespace eNote.Tests.Rentals;

public sealed class RentalQueryServiceTests
{
    private static readonly DateTime Now = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetPagedForStudentAsync_ReturnsOnlyOwnRentals()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student1 = await SeedStudentAsync(context, appUserId: 100);
        var student2 = await SeedStudentAsync(context, appUserId: 200);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        context.Set<InstrumentRental>().Add(new InstrumentRental(instrument.Id, student1.Id, instrument.MusicStoreId, Now, null));
        context.Set<InstrumentRental>().Add(new InstrumentRental(instrument.Id, student2.Id, instrument.MusicStoreId, Now, null));
        await context.SaveChangesAsync();
        var service = CreateService(context, student1);

        var result = await service.GetPagedForStudentAsync(new InstrumentRentalSearchObject { PageSize = 10 });

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(student1.Id, result.Items[0].StudentProfileId);
    }

    [Fact]
    public async Task GetPagedForStoreAsync_ReturnsAllStoreRentals()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student1 = await SeedStudentAsync(context, appUserId: 100);
        var student2 = await SeedStudentAsync(context, appUserId: 200);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        context.Set<InstrumentRental>().Add(new InstrumentRental(instrument.Id, student1.Id, instrument.MusicStoreId, Now, null));
        context.Set<InstrumentRental>().Add(new InstrumentRental(instrument.Id, student2.Id, instrument.MusicStoreId, Now, null));
        await context.SaveChangesAsync();
        var service = CreateService(context, student1);

        var result = await service.GetPagedForStoreAsync(new InstrumentRentalSearchObject { PageSize = 10 });

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetByIdForStudentAsync_Throws_WhenRentalBelongsToAnotherStudent()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student1 = await SeedStudentAsync(context, appUserId: 100);
        var student2 = await SeedStudentAsync(context, appUserId: 200);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = new InstrumentRental(instrument.Id, student2.Id, instrument.MusicStoreId, Now, null);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();
        var service = CreateService(context, student1);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdForStudentAsync(rental.Id));
    }

    [Fact]
    public async Task GetByIdForStudentAsync_AppliesBilling_WhenRentalIsActive()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await SeedStudentAsync(context, appUserId: 100);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now.AddDays(-35), null);
        RentalTestData.AdvanceTo(rental, InstrumentRentalStatus.Active, Now, approvedAt: Now.AddDays(-35), pickedUpAt: Now.AddDays(-30));
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        var dto = await service.GetByIdForStudentAsync(rental.Id);

        Assert.Equal(50m, dto.Fee);
        Assert.Equal(30, dto.DaysCharged);
        Assert.Equal(50.00m, dto.TotalFee);
        Assert.Equal(0, dto.OverdueDays);
    }

    [Fact]
    public async Task GetByIdForStudentAsync_ComputesTermAndOverduePenalty()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await SeedStudentAsync(context, appUserId: 100);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now.AddDays(-35), null);
        RentalTestData.AdvanceTo(rental, InstrumentRentalStatus.Active, Now, fee: 90m, approvedAt: Now.AddDays(-35), pickedUpAt: Now.AddDays(-34));
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        var dto = await service.GetByIdForStudentAsync(rental.Id);

        Assert.Equal(Now.AddDays(-4), dto.DueAt);
        Assert.Equal(Now.AddDays(-9), dto.RenewalOpensAt);
        Assert.Equal(1.50m, dto.DailyPenaltyFee);
        Assert.Equal(4, dto.OverdueDays);
        Assert.Equal(6.00m, dto.PenaltyFee);
        Assert.Equal(108.00m, dto.TotalFee);
    }

    [Fact]
    public async Task GetDebtForStudentAsync_ReturnsDebt_WhenInstrumentSoftDeleted()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await SeedStudentAsync(context, appUserId: 100);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var unpaid = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now.AddDays(-20), null);
        RentalTestData.AdvanceTo(unpaid, InstrumentRentalStatus.Completed, Now, approvedAt: Now.AddDays(-35), pickedUpAt: Now.AddDays(-35), finishedAt: Now.AddDays(-5));
        context.Set<InstrumentRental>().Add(unpaid);
        await context.SaveChangesAsync();

        instrument.SoftDelete();
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        var debt = await service.GetDebtForStudentAsync();

        Assert.True(debt.HasUnpaidDebt);
        Assert.Equal(unpaid.Id, debt.RentalId);
    }

    [Fact]
    public async Task GetByIdForStudentAsync_ReturnsRental_WhenInstrumentSoftDeleted()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await SeedStudentAsync(context, appUserId: 100);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = RentalTestData.CreateCompletedRental(instrument, student.Id, Now);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();

        instrument.SoftDelete();
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        var dto = await service.GetByIdForStudentAsync(rental.Id);

        Assert.Equal(rental.Id, dto.Id);
        Assert.False(dto.InstrumentIsActive);
    }

    [Fact]
    public async Task GetByIdForStudentAsync_ReturnsPickupDeadline_OnlyWhileApproved()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await SeedStudentAsync(context, appUserId: 100);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var approved = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, new DateTime(2026, 6, 13, 10, 0, 0, DateTimeKind.Utc), null);
        RentalTestData.AdvanceTo(approved, InstrumentRentalStatus.Approved, Now, approvedAt: new DateTime(2026, 6, 14, 10, 0, 0, DateTimeKind.Utc));
        var activeInstrument = new Instrument("Second Model", "Second Manufacturer", null, null, instrument.InstrumentTypeId, instrument.MusicStoreId);
        context.Set<Instrument>().Add(activeInstrument);
        await context.SaveChangesAsync();
        var active = new InstrumentRental(activeInstrument.Id, student.Id, activeInstrument.MusicStoreId, new DateTime(2026, 6, 13, 10, 0, 0, DateTimeKind.Utc), null);
        RentalTestData.AdvanceTo(active, InstrumentRentalStatus.Active, Now, approvedAt: new DateTime(2026, 6, 14, 10, 0, 0, DateTimeKind.Utc), pickedUpAt: new DateTime(2026, 6, 14, 11, 0, 0, DateTimeKind.Utc));
        context.Set<InstrumentRental>().AddRange(approved, active);
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        var approvedDto = await service.GetByIdForStudentAsync(approved.Id);
        var activeDto = await service.GetByIdForStudentAsync(active.Id);

        Assert.Equal(new DateTime(2026, 6, 16, 10, 0, 0, DateTimeKind.Utc), approvedDto.PickupDeadline);
        Assert.Null(activeDto.PickupDeadline);
    }

    [Fact]
    public async Task GetPagedForStudentAsync_FiltersByRentalStatusAndInstrumentId()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await SeedStudentAsync(context, appUserId: 100);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var secondInstrument = new Instrument("Second Model", "Second Maker", null, null, instrument.InstrumentTypeId, instrument.MusicStoreId);
        context.Set<Instrument>().Add(secondInstrument);
        await context.SaveChangesAsync();

        var pending = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now.AddDays(-3), null);
        var completed = RentalTestData.CreateCompletedRental(instrument, student.Id, Now);
        var secondInstrumentPending = new InstrumentRental(secondInstrument.Id, student.Id, secondInstrument.MusicStoreId, Now.AddDays(-2), null);
        context.Set<InstrumentRental>().AddRange(pending, completed, secondInstrumentPending);
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        var byInstrument = await service.GetPagedForStudentAsync(new InstrumentRentalSearchObject { InstrumentId = instrument.Id });
        var byStatus = await service.GetPagedForStudentAsync(new InstrumentRentalSearchObject { RentalStatus = InstrumentRentalStatus.Pending });

        Assert.Equal(
            new[] { pending.Id, completed.Id }.Order(),
            byInstrument.Items.Select(x => x.Id).Order());
        Assert.Equal(
            new[] { pending.Id, secondInstrumentPending.Id }.Order(),
            byStatus.Items.Select(x => x.Id).Order());
    }

    [Fact]
    public async Task GetPagedForStudentAsync_OrdersByRequestedAtDescending_ThenById_AndPages()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await SeedStudentAsync(context, appUserId: 100);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var tiedAt = Now.AddDays(-1);
        var third = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, tiedAt, null) { Id = 3 };
        var first = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, tiedAt, null) { Id = 1 };
        var second = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, tiedAt, null) { Id = 2 };
        var oldest = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now.AddDays(-5), null) { Id = 4 };
        context.Set<InstrumentRental>().Add(third);
        await context.SaveChangesAsync();
        context.Set<InstrumentRental>().Add(first);
        await context.SaveChangesAsync();
        context.Set<InstrumentRental>().Add(second);
        await context.SaveChangesAsync();
        context.Set<InstrumentRental>().Add(oldest);
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        var firstPage = await service.GetPagedForStudentAsync(new InstrumentRentalSearchObject { Page = 1, PageSize = 2 });
        var secondPage = await service.GetPagedForStudentAsync(new InstrumentRentalSearchObject { Page = 2, PageSize = 2 });

        Assert.Equal([1, 2], firstPage.Items.Select(x => x.Id));
        Assert.Equal([3, 4], secondPage.Items.Select(x => x.Id));
    }

    [Fact]
    public async Task GetDebtForStudentAsync_PaidCompletedRental_IsNoDebt()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await SeedStudentAsync(context, appUserId: 100);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = RentalTestData.CreateCompletedRental(instrument, student.Id, Now);
        rental.MarkPaid(5000, Now);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        var debt = await service.GetDebtForStudentAsync();

        Assert.False(debt.HasUnpaidDebt);
        Assert.Null(debt.RentalId);
    }

    [Fact]
    public async Task GetDebtForStudentAsync_RefundOnPaidRental_DoesNotRestoreDebt()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await SeedStudentAsync(context, appUserId: 100);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = RentalTestData.CreateCompletedRental(instrument, student.Id, Now);
        rental.MarkPaid(5000, Now);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();
        var payment = new RentalPayment(rental.Id, rental.MusicStoreId, "pi_rental_refund", 5000, "bam", PaymentStatus.Succeeded);
        payment.MarkSucceeded("ch_refund", "evt_refund", Now);
        payment.ApplyRefund(5000, "re_refund", Now);
        context.Set<RentalPayment>().Add(payment);
        await context.SaveChangesAsync();
        Assert.Equal(rental.Id, payment.InstrumentRentalId);
        var service = CreateService(context, student);

        var debt = await service.GetDebtForStudentAsync();

        Assert.False(debt.HasUnpaidDebt);
        Assert.Null(debt.RentalId);
    }

    [Theory]
    [InlineData(InstrumentRentalStatus.Pending)]
    [InlineData(InstrumentRentalStatus.Approved)]
    [InlineData(InstrumentRentalStatus.Active)]
    public async Task GetDebtForStudentAsync_OpenRental_IsNoDebt(InstrumentRentalStatus status)
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await SeedStudentAsync(context, appUserId: 100);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now, null);
        RentalTestData.AdvanceTo(rental, status, Now);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        var debt = await service.GetDebtForStudentAsync();

        Assert.False(debt.HasUnpaidDebt);
    }

    [Fact]
    public async Task GetDebtForStudentAsync_IgnoresAnotherStudentsUnpaidRental()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await SeedStudentAsync(context, appUserId: 100);
        var otherStudent = await SeedStudentAsync(context, appUserId: 200);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = new InstrumentRental(instrument.Id, otherStudent.Id, instrument.MusicStoreId, Now.AddDays(-45), null);
        RentalTestData.AdvanceTo(rental, InstrumentRentalStatus.Completed, Now, approvedAt: Now.AddDays(-45), pickedUpAt: Now.AddDays(-41), finishedAt: Now.AddDays(-10));
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        var debt = await service.GetDebtForStudentAsync();

        Assert.False(debt.HasUnpaidDebt);
    }

    [Fact]
    public async Task GetDebtForStudentAsync_ReturnsOldestRequestedAt_WhenTwoDebts()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await SeedStudentAsync(context, appUserId: 100);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var secondInstrument = new Instrument("Second Model", "Second Maker", null, null, instrument.InstrumentTypeId, instrument.MusicStoreId);
        context.Set<Instrument>().Add(secondInstrument);
        await context.SaveChangesAsync();

        var older = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now.AddDays(-60), null);
        RentalTestData.AdvanceTo(older, InstrumentRentalStatus.Completed, Now, approvedAt: Now.AddDays(-60), pickedUpAt: Now.AddDays(-60), finishedAt: Now.AddDays(-10));
        var newer = new InstrumentRental(secondInstrument.Id, student.Id, secondInstrument.MusicStoreId, Now.AddDays(-40), null);
        RentalTestData.AdvanceTo(newer, InstrumentRentalStatus.Completed, Now, approvedAt: Now.AddDays(-40), pickedUpAt: Now.AddDays(-40), finishedAt: Now.AddDays(-1));
        context.Set<InstrumentRental>().AddRange(older, newer);
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        var debt = await service.GetDebtForStudentAsync();

        Assert.True(debt.HasUnpaidDebt);
        Assert.Equal(older.Id, debt.RentalId);
    }

    [Fact]
    public async Task GetByIdForStoreAsync_ReturnsStudentName()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await SeedStudentAsync(context, appUserId: 100);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = RentalTestData.CreateCompletedRental(instrument, student.Id, Now);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        var dto = await service.GetByIdForStoreAsync(rental.Id);

        Assert.Equal($"Student {student.Id}", dto.StudentName);
    }

    [Fact]
    public async Task GetPagedForStoreAsync_ReturnsArchivedInstrumentRental_WithModelAndInactiveFlag()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var student = await SeedStudentAsync(context, appUserId: 100);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = RentalTestData.CreateCompletedRental(instrument, student.Id, Now);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();
        instrument.SoftDelete();
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        var result = await service.GetPagedForStoreAsync(new InstrumentRentalSearchObject { PageSize = 10 });

        var dto = Assert.Single(result.Items);
        Assert.Equal(rental.Id, dto.Id);
        Assert.Equal("Stradivarius", dto.InstrumentModel);
        Assert.False(dto.InstrumentIsActive);
    }

    private static async Task<Student> SeedStudentAsync(ENoteContext context, int appUserId)
    {
        var student = new Student(appUserId, Now.AddMonths(-1));
        student.UpdateMembership(Now.AddDays(1));
        context.Set<Student>().Add(student);
        await context.SaveChangesAsync();
        return student;
    }

    private static RentalQueryService CreateService(ENoteContext context, Student student) =>
        new(context, TestMapper.Create(), new StubCurrentActor(student: student, storeId: 1), new FixedClock(Now), new StubDisplayNameService());
}
