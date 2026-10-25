using eNote.API.Controllers.InstrumentRentals;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Application.Constants;
using eNote.Application.Features.Rentals.InstrumentRentals.Services;
using eNote.Application.Features.Rentals.Payments.Services;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace eNote.Tests.Rentals;

public sealed class RentalPaymentServiceTests
{
    private static readonly DateTime Now = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreatePaymentIntent_FromPending_Throws_NotPayable()
    {
        var (context, student, currentUser) = await CreateStudentContextAsync();
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now, null);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();
        var service = CreateService(context, currentUser);

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.CreatePaymentIntentAsync(rental.Id));

        Assert.Equal(Messages.PaymentNotPayableInStatus, ex.Message);
    }

    [Fact]
    public async Task CreatePaymentIntent_FromApproved_Throws_NotPayable()
    {
        var (context, student, currentUser) = await CreateStudentContextAsync();
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now, null);
        rental.Approve(50m, null, Now, 1);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();
        var service = CreateService(context, currentUser);

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.CreatePaymentIntentAsync(rental.Id));

        Assert.Equal(Messages.PaymentNotPayableInStatus, ex.Message);
    }

    [Fact]
    public async Task CreatePaymentIntent_FromActive_Throws_NotPayable()
    {
        var (context, student, currentUser) = await CreateStudentContextAsync();
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now, null);
        rental.Approve(50m, null, Now, 1);
        rental.Pickup(Now);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();
        var service = CreateService(context, currentUser);

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.CreatePaymentIntentAsync(rental.Id));

        Assert.Equal(Messages.PaymentNotPayableInStatus, ex.Message);
    }

    [Fact]
    public async Task CreatePaymentIntent_FromCompleted_Succeeds()
    {
        var (context, student, currentUser) = await CreateStudentContextAsync();
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = RentalTestData.CreateCompletedRental(instrument, student.Id, Now);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();
        var gateway = new FakePaymentGateway();
        var service = CreateService(context, currentUser, gateway);

        var result = await service.CreatePaymentIntentAsync(rental.Id);

        Assert.Equal(rental.Id, result.RentalId);
        Assert.Equal("pi_test_1", result.PaymentIntentId);
        Assert.Equal(1000, result.AmountCents);
        Assert.Equal("eur", result.Currency);
        Assert.Equal(PaymentStatus.RequiresAction, result.Status);
        Assert.False(string.IsNullOrWhiteSpace(result.ClientSecret));
        Assert.Single(gateway.CreateCalls);
        Assert.Equal(1000, gateway.CreateCalls[0].AmountCents);
        Assert.Equal("eur", gateway.CreateCalls[0].Currency);
    }

    [Fact]
    public async Task CreatePaymentIntent_FromReturnedEarly_Succeeds()
    {
        var (context, student, currentUser) = await CreateStudentContextAsync();
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = CreateReturnedEarlyRental(instrument, student.Id);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();
        var service = CreateService(context, currentUser);

        var result = await service.CreatePaymentIntentAsync(rental.Id);

        Assert.NotNull(result);
        Assert.Equal(500, result.AmountCents);
        Assert.Equal(PaymentStatus.RequiresAction, result.Status);
    }

    [Fact]
    public async Task CreatePaymentIntent_ComputesCentsFromServerCharges_IgnoresClientAmount()
    {
        var (context, student, currentUser) = await CreateStudentContextAsync();
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = RentalTestData.CreateCompletedRental(instrument, student.Id, Now);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();
        var gateway = new FakePaymentGateway();
        var service = CreateService(context, currentUser, gateway);

        var result = await service.CreatePaymentIntentAsync(rental.Id);

        Assert.Equal(1000, result.AmountCents);
        Assert.Equal("eur", result.Currency);
        Assert.Equal(1000, gateway.CreateCalls[0].AmountCents);
        Assert.Equal("eur", gateway.CreateCalls[0].Currency);
    }

    [Fact]
    public async Task CreatePaymentIntent_Idempotent_SecondCallReturnsSameIntent()
    {
        var (context, student, currentUser) = await CreateStudentContextAsync();
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = RentalTestData.CreateCompletedRental(instrument, student.Id, Now);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();
        var gateway = new FakePaymentGateway();
        var service = CreateService(context, currentUser, gateway);

        var first = await service.CreatePaymentIntentAsync(rental.Id);
        var second = await service.CreatePaymentIntentAsync(rental.Id);

        Assert.Equal(first.PaymentIntentId, second.PaymentIntentId);
        Assert.Single(gateway.CreateCalls);
        Assert.Single(gateway.RetrieveCalls);
        Assert.Equal(1, await context.Set<RentalPayment>().CountAsync());
    }

    [Fact]
    public async Task CreatePaymentIntent_ReusesOpenIntent_RegardlessOfAge()
    {
        var (context, student, currentUser) = await CreateStudentContextAsync();
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = RentalTestData.CreateCompletedRental(instrument, student.Id, Now);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();
        var gateway = new FakePaymentGateway();
        var clock = new MutableClock(Now);
        var service = CreateService(context, currentUser, gateway, clock: clock);

        var first = await service.CreatePaymentIntentAsync(rental.Id);
        clock.UtcNow = Now.AddHours(5);
        var second = await service.CreatePaymentIntentAsync(rental.Id);

        Assert.Equal(first.PaymentIntentId, second.PaymentIntentId);
        Assert.Equal(PaymentStatus.RequiresAction, second.Status);
        Assert.Single(gateway.CreateCalls);
        Assert.Single(gateway.RetrieveCalls);
        Assert.Empty(gateway.CancelCalls);
        Assert.Equal(1, await context.Set<RentalPayment>().CountAsync());
    }

    [Theory]
    [InlineData("processing")]
    [InlineData("succeeded")]
    public async Task CreatePaymentIntent_Blocks_WhenStripeIsPastTheCardStep(string stripeStatus)
    {
        var (context, student, currentUser) = await CreateStudentContextAsync();
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = RentalTestData.CreateCompletedRental(instrument, student.Id, Now);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();
        var gateway = new FakePaymentGateway();
        var service = CreateService(context, currentUser, gateway);

        await service.CreatePaymentIntentAsync(rental.Id);
        gateway.SetIntentStatus("pi_test_1", stripeStatus);

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.CreatePaymentIntentAsync(rental.Id));

        Assert.Equal(Messages.PaymentProcessing, ex.Message);
        Assert.Single(gateway.CreateCalls);
        Assert.Empty(gateway.CancelCalls);
        var payment = await context.Set<RentalPayment>().SingleAsync();
        Assert.Equal(PaymentStatus.RequiresAction, payment.Status);
    }

    [Fact]
    public async Task CreatePaymentIntent_DuplicateIntentRow_ReturnsStoredWinner()
    {
        var (context, student, currentUser) = await CreateStudentContextAsync();
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = RentalTestData.CreateCompletedRental(instrument, student.Id, Now);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();
        var winner = new RentalPayment(rental.Id, rental.MusicStoreId, "pi_test_1", 1000, "eur", PaymentStatus.Succeeded);
        context.Set<RentalPayment>().Add(winner);
        await context.SaveChangesAsync();
        var gateway = new FakePaymentGateway();
        var inner = new Exception($"duplicate key value violates unique constraint \"{DbConstraintNames.RentalPaymentStripePaymentIntentIdUniqueIndex}\"");
        var throwing = new ThrowingSaveDbContext(context, new DbUpdateException("Unique constraint violated.", inner));
        var service = CreateService(throwing, currentUser, gateway);

        var result = await service.CreatePaymentIntentAsync(rental.Id);

        Assert.Equal("pi_test_1", result.PaymentIntentId);
        Assert.Equal("pi_test_1_secret", result.ClientSecret);
        Assert.Equal(1000, result.AmountCents);
        Assert.Equal("eur", result.Currency);
        Assert.Equal(PaymentStatus.Succeeded, result.Status);
        Assert.Empty(gateway.CancelCalls);
    }

    [Fact]
    public async Task CreatePaymentIntent_OpenAttemptRace_CancelsLosingIntent_AndReturnsWinner()
    {
        var databaseName = Guid.NewGuid().ToString();
        var (context, student, currentUser) = await CreateStudentContextAsync(databaseName: databaseName);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = RentalTestData.CreateCompletedRental(instrument, student.Id, Now);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();
        var gateway = new FakePaymentGateway();
        gateway.AddIntent("pi_winner", 2000, "eur");
        var parallel = CreateContext(currentUser, databaseName);
        var inner = new Exception($"duplicate key value violates unique constraint \"{DbConstraintNames.RentalPaymentOpenAttemptUniqueIndex}\"");
        var throwing = new ThrowingSaveDbContext(context, new DbUpdateException("Unique constraint violated.", inner), async () =>
        {
            parallel.Set<RentalPayment>().Add(new RentalPayment(rental.Id, rental.MusicStoreId, "pi_winner", 2000, "eur", PaymentStatus.RequiresAction));
            await parallel.SaveChangesAsync();
        });
        var service = CreateService(throwing, currentUser, gateway);

        var result = await service.CreatePaymentIntentAsync(rental.Id);

        Assert.Equal("pi_winner", result.PaymentIntentId);
        Assert.Equal("pi_winner_secret", result.ClientSecret);
        Assert.Equal(2000, result.AmountCents);
        Assert.Equal("eur", result.Currency);
        Assert.Equal(PaymentStatus.RequiresAction, result.Status);
        Assert.Single(gateway.CancelCalls);
        Assert.Equal("pi_test_1", gateway.CancelCalls[0].PaymentIntentId);
    }

    [Fact]
    public async Task CreatePaymentIntent_AlreadyPaid_Throws_PaymentAlreadyCompleted()
    {
        var (context, student, currentUser) = await CreateStudentContextAsync();
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = RentalTestData.CreateCompletedRental(instrument, student.Id, Now);
        rental.MarkPaid(5000, Now);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();
        var gateway = new FakePaymentGateway();
        var service = CreateService(context, currentUser, gateway);

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.CreatePaymentIntentAsync(rental.Id));

        Assert.Equal(Messages.PaymentAlreadyCompleted, ex.Message);
        Assert.Empty(gateway.CreateCalls);
    }

    [Fact]
    public async Task Refund_Fails_WhenNoSucceededPayment()
    {
        var (context, student, _) = await CreateStudentContextAsync();
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = RentalTestData.CreateCompletedRental(instrument, student.Id, Now);
        rental.MarkPaid(5000, Now);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();
        var service = CreateService(context, CreateStoreActor());

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.RefundAsync(rental.Id, null));

        Assert.Equal(Messages.PaymentNotFound, ex.Message);
    }

    [Fact]
    public async Task Refund_Full_SetsRefunded()
    {
        var (context, student, _) = await CreateStudentContextAsync();
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = await SeedPaidRentalAsync(context, instrument, student);
        var gateway = new FakePaymentGateway();
        var storeActor = CreateStoreActor();
        var dispatcher = new RecordingNotificationDispatcher();
        var service = CreateService(context, storeActor, gateway, dispatcher);

        var dto = await service.RefundAsync(rental.Id, null);

        Assert.Equal(PaymentStatus.Refunded, dto.Status);
        Assert.Equal(5000, dto.RefundedCents);
        var payment = await context.Set<RentalPayment>().SingleAsync(x => x.StripePaymentIntentId == "pi_test_1");
        Assert.Equal("re_test_1", payment.StripeRefundId);
        Assert.Single(gateway.RefundCalls);
        Assert.Equal("pi_test_1", gateway.RefundCalls[0].PaymentIntentId);
        Assert.Equal(5000, gateway.RefundCalls[0].AmountCents);
        Assert.Single(dispatcher.RefundCalls);
        Assert.Equal(5000, dispatcher.RefundCalls[0].RefundedCents);
        Assert.Equal("eur", dispatcher.RefundCalls[0].Currency);
        Assert.Equal(storeActor.UserId, dispatcher.RefundCalls[0].ActorUserId);
    }

    [Fact]
    public async Task Refund_Partial_LeavesPartiallyRefunded()
    {
        var (context, student, _) = await CreateStudentContextAsync();
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = await SeedPaidRentalAsync(context, instrument, student);
        var gateway = new FakePaymentGateway();
        var service = CreateService(context, CreateStoreActor(), gateway);

        var dto = await service.RefundAsync(rental.Id, 2000);

        Assert.Equal(PaymentStatus.PartiallyRefunded, dto.Status);
        Assert.Equal(2000, dto.RefundedCents);
        Assert.NotNull(dto.RefundedAt);

        var second = await service.RefundAsync(rental.Id, 1500);

        Assert.Equal(PaymentStatus.PartiallyRefunded, second.Status);
        Assert.Equal(3500, second.RefundedCents);
        Assert.Equal(2, gateway.RefundCalls.Count);
        Assert.NotEqual(gateway.RefundCalls[0].IdempotencyKey, gateway.RefundCalls[1].IdempotencyKey);
    }

    [Fact]
    public async Task Refund_RetryAfterLostResponse_ReusesIdempotencyKey()
    {
        var (context, student, _) = await CreateStudentContextAsync();
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = await SeedPaidRentalAsync(context, instrument, student);
        var gateway = new FakePaymentGateway { RefundException = new InvalidOperationException("lost response") };
        var service = CreateService(context, CreateStoreActor(), gateway);

        await Assert.ThrowsAnyAsync<Exception>(() => service.RefundAsync(rental.Id, 2000));
        await Assert.ThrowsAnyAsync<Exception>(() => service.RefundAsync(rental.Id, 2000));

        Assert.Equal(2, gateway.RefundCalls.Count);
        Assert.Equal(gateway.RefundCalls[0].IdempotencyKey, gateway.RefundCalls[1].IdempotencyKey);
    }

    [Fact]
    public async Task Refund_WhenGatewayThrows_Throws_PaymentProviderUnavailable()
    {
        var (context, student, _) = await CreateStudentContextAsync();
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = await SeedPaidRentalAsync(context, instrument, student);
        var service = CreateService(context, CreateStoreActor(), new ThrowingPaymentGateway());

        var ex = await Assert.ThrowsAsync<PaymentProviderUnavailableException>(() => service.RefundAsync(rental.Id, 2000));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal("error.payment_provider_unavailable", ex.ErrorCode);
        Assert.Equal(Messages.PaymentProviderUnavailable, ex.Message);
    }

    [Fact]
    public async Task Refund_Pending_LeavesPaymentUnchanged_AndDoesNotNotify()
    {
        var (context, student, _) = await CreateStudentContextAsync();
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = await SeedPaidRentalAsync(context, instrument, student);
        var gateway = new FakePaymentGateway { RefundStatus = "pending" };
        var dispatcher = new RecordingNotificationDispatcher();
        var service = CreateService(context, CreateStoreActor(), gateway, dispatcher);

        var dto = await service.RefundAsync(rental.Id, 2000);

        Assert.Equal(PaymentStatus.Succeeded, dto.Status);
        Assert.Null(dto.RefundedCents);
        var payment = await context.Set<RentalPayment>().SingleAsync(x => x.StripePaymentIntentId == "pi_test_1");
        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        Assert.Null(payment.RefundedCents);
        Assert.Single(gateway.RefundCalls);
        Assert.Empty(dispatcher.RefundCalls);
    }

    [Fact]
    public async Task Refund_Failed_Throws_RefundFailed()
    {
        var (context, student, _) = await CreateStudentContextAsync();
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = await SeedPaidRentalAsync(context, instrument, student);
        var gateway = new FakePaymentGateway { RefundStatus = "failed" };
        var service = CreateService(context, CreateStoreActor(), gateway);

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.RefundAsync(rental.Id, 2000));

        Assert.Equal(Messages.RefundFailed, ex.Message);
        var payment = await context.Set<RentalPayment>().SingleAsync(x => x.StripePaymentIntentId == "pi_test_1");
        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        Assert.Null(payment.RefundedCents);
    }

    [Fact]
    public async Task Refund_ExceedsCharged_Throws()
    {
        var (context, student, _) = await CreateStudentContextAsync();
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = await SeedPaidRentalAsync(context, instrument, student);
        var gateway = new FakePaymentGateway();
        var service = CreateService(context, CreateStoreActor(), gateway);

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.RefundAsync(rental.Id, 5001));

        Assert.Equal(Messages.RefundExceedsCharged, ex.Message);
        Assert.Empty(gateway.RefundCalls);
    }

    [Fact]
    public async Task Refund_Succeeds_IsPaidRemainsTrue()
    {
        var (context, student, _) = await CreateStudentContextAsync();
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = await SeedPaidRentalAsync(context, instrument, student);
        var service = CreateService(context, CreateStoreActor());

        var dto = await service.RefundAsync(rental.Id, null);

        Assert.Equal(PaymentStatus.Refunded, dto.Status);
        var reloaded = await context.Set<InstrumentRental>().SingleAsync(x => x.Id == rental.Id);
        Assert.True(reloaded.IsPaid);
        Assert.NotNull(reloaded.PaidAt);
        Assert.Equal(50m, reloaded.AmountPaid);
    }

    [Fact]
    public async Task GetPaymentStatusForStore_Succeeds_ReturnsLatestPayment()
    {
        var (context, student, _) = await CreateStudentContextAsync();
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = await SeedPaidRentalAsync(context, instrument, student);
        var service = CreateService(context, CreateStoreActor());

        var dto = await service.GetPaymentStatusForStoreAsync(rental.Id);

        Assert.Equal(rental.Id, dto.RentalId);
        Assert.Equal(PaymentStatus.Succeeded, dto.Status);
        Assert.Equal(5000, dto.AmountCents);
    }

    [Fact]
    public async Task GetPaymentStatusForStore_AfterRefund_ReturnsRefundedStatus()
    {
        var (context, student, _) = await CreateStudentContextAsync();
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = await SeedPaidRentalAsync(context, instrument, student);
        var service = CreateService(context, CreateStoreActor());

        await service.RefundAsync(rental.Id, null);
        var dto = await service.GetPaymentStatusForStoreAsync(rental.Id);

        Assert.Equal(PaymentStatus.Refunded, dto.Status);
        Assert.Equal(5000, dto.RefundedCents);
        Assert.NotNull(dto.RefundedAt);
    }

    [Fact]
    public async Task GetPaymentStatusForStore_NoPayment_ThrowsNotFound()
    {
        var (context, student, _) = await CreateStudentContextAsync();
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = RentalTestData.CreateCompletedRental(instrument, student.Id, Now);
        rental.MarkPaid(5000, Now);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();

        var gateway = new FakePaymentGateway();
        var service = CreateService(context, CreateStoreActor(), gateway);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.GetPaymentStatusForStoreAsync(rental.Id));

        Assert.Equal(Messages.PaymentNotFound, ex.Message);
    }

    [Fact]
    public async Task Student_CanCreateIntent_ForOwnRental()
    {
        var (context, student, currentUser) = await CreateStudentContextAsync();
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = RentalTestData.CreateCompletedRental(instrument, student.Id, Now);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();
        var service = CreateService(context, currentUser);

        var result = await service.CreatePaymentIntentAsync(rental.Id);

        Assert.NotNull(result);
        Assert.Equal(rental.Id, result.RentalId);
    }

    [Fact]
    public async Task Student_CannotCreateForOthersRental()
    {
        var (context, studentA, currentUserA) = await CreateStudentContextAsync(appUserId: 100);
        var studentB = await SeedStudentAsync(context, appUserId: 200);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = RentalTestData.CreateCompletedRental(instrument, studentB.Id, Now);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();
        var service = CreateService(context, currentUserA);

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.CreatePaymentIntentAsync(rental.Id));

        Assert.Equal(Messages.RentalAccessDenied, ex.Message);
    }

    [Fact]
    public async Task StoreEmployee_CannotCreateIntent()
    {
        var (context, student, _) = await CreateStudentContextAsync();
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = RentalTestData.CreateCompletedRental(instrument, student.Id, Now);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();
        var service = CreateService(context, CreateStoreActor());

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.CreatePaymentIntentAsync(rental.Id));

        Assert.Equal(Messages.RentalAccessDenied, ex.Message);
    }

    [Fact]
    public async Task StoreEmployee_CanRefund()
    {
        var (context, student, _) = await CreateStudentContextAsync();
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = await SeedPaidRentalAsync(context, instrument, student);
        var service = CreateService(context, CreateStoreActor());

        var dto = await service.RefundAsync(rental.Id, null);

        Assert.Equal(PaymentStatus.Refunded, dto.Status);
    }

    [Fact]
    public async Task CreatePaymentIntent_WhenGatewayThrows_ServiceThrows_PaymentProviderUnavailable()
    {
        var (context, student, currentUser) = await CreateStudentContextAsync();
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = RentalTestData.CreateCompletedRental(instrument, student.Id, Now);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();
        var service = CreateService(context, currentUser, new ThrowingPaymentGateway());

        var ex = await Assert.ThrowsAsync<PaymentProviderUnavailableException>(
            () => service.CreatePaymentIntentAsync(rental.Id));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal("error.payment_provider_unavailable", ex.ErrorCode);
        Assert.Equal(Messages.PaymentProviderUnavailable, ex.Message);
    }

    [Fact]
    public async Task CreatePaymentIntent_WhenGatewayThrows_ControllerSurfaces_TypedError()
    {
        var (context, student, currentUser) = await CreateStudentContextAsync();
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = RentalTestData.CreateCompletedRental(instrument, student.Id, Now);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();
        var service = CreateService(context, currentUser, new ThrowingPaymentGateway());
        var controller = new RentalPaymentsController(service);

        var ex = await Assert.ThrowsAsync<PaymentProviderUnavailableException>(
            () => controller.CreateIntent(rental.Id, CancellationToken.None));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal("error.payment_provider_unavailable", ex.ErrorCode);
    }

    private static ENoteContext CreateContext(StubCurrentActor currentUser, string? databaseName = null)
    {
        return new ENoteContext(TestDbContextFactory.CreateOptions(databaseName), new FixedClock(Now), currentUser) { ExplicitStoreId = 1 };
    }

    private static async Task<(ENoteContext Context, Student Student, StubCurrentActor CurrentUser)> CreateStudentContextAsync(int appUserId = 100, string? databaseName = null)
    {
        var student = new Student(appUserId, Now.AddMonths(-1));
        student.UpdateMembership(Now.AddDays(1));
        var currentUser = new StubCurrentActor(student: student);
        var context = CreateContext(currentUser, databaseName);
        context.Set<Student>().Add(student);
        await context.SaveChangesAsync();
        return (context, student, currentUser);
    }

    private static async Task<Student> SeedStudentAsync(ENoteContext context, int appUserId = 100)
    {
        var student = new Student(appUserId, Now.AddMonths(-1));
        student.UpdateMembership(Now.AddDays(1));
        context.Set<Student>().Add(student);
        await context.SaveChangesAsync();
        return student;
    }

    private static InstrumentRental CreateReturnedEarlyRental(Instrument instrument, int studentId)
    {
        var rental = new InstrumentRental(instrument.Id, studentId, instrument.MusicStoreId, Now.AddDays(-10), null);
        rental.Approve(50m, null, Now.AddDays(-9), 1);
        rental.Pickup(Now.AddDays(-9));
        rental.ReturnEarly(Now.AddDays(-6), null);
        return rental;
    }

    private static async Task<InstrumentRental> SeedPaidRentalAsync(ENoteContext context, Instrument instrument, Student student)
    {
        var rental = RentalTestData.CreateCompletedRental(instrument, student.Id, Now);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();

        rental.MarkPaid(5000, Now);
        context.Set<RentalPayment>().Add(new RentalPayment(rental.Id, rental.MusicStoreId, "pi_test_1", 5000, "eur", PaymentStatus.Succeeded));
        await context.SaveChangesAsync();
        return rental;
    }

    private static StubCurrentActor CreateStoreActor() => new(storeId: 1);

    private static RentalPaymentService CreateService(
        IAppDbContext context,
        StubCurrentActor currentUser,
        IPaymentGateway? gateway = null,
        IRentalNotificationDispatcher? dispatcher = null,
        IClock? clock = null)
    {
        return new(
            context,
            TestMapper.Create(),
            clock ?? new FixedClock(Now),
            currentUser,
            currentUser,
            gateway ?? new FakePaymentGateway(),
            dispatcher ?? new NoOpNotificationDispatcher(),
            new StripeOptions { Currency = "eur" },
            NullLogger<RentalPaymentService>.Instance);
    }

    private sealed class MutableClock(DateTime initial) : IClock
    {
        public DateTime UtcNow { get; set; } = initial;
    }
}
