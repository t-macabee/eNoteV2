using eNote.Application.Common.Localization;
using eNote.Application.Features.Rentals.Payments.Services;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace eNote.Tests.Payments;

public sealed class PaymentGatewayHelpersTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ResolveOpenAttemptsAsync_PersistsCanceledRows()
    {
        var databaseName = Guid.NewGuid().ToString();
        var (context, _, _, enrollment) = await TuitionTestData.SetupScenarioAsync(Now, price: 100m, databaseName);
        var gateway = new FakePaymentGateway();
        gateway.AddIntent("pi_open", 10000, "bam");
        var payment = new CoursePayment(enrollment.Id, "pi_open", 10000, "bam", PaymentStatus.RequiresAction);
        context.Set<CoursePayment>().Add(payment);
        await context.SaveChangesAsync();

        await PaymentGatewayHelpers.ResolveOpenAttemptsAsync(
            NullLogger.Instance,
            context,
            gateway,
            [payment],
            expected: null,
            Messages.PaymentProcessing,
            CancellationToken.None);

        var fresh = TestDbContextFactory.CreateContext(Now, databaseName);
        var stored = await fresh.Set<CoursePayment>().SingleAsync(p => p.Id == payment.Id);

        Assert.Equal(PaymentStatus.Canceled, stored.Status);
        Assert.Single(gateway.CancelCalls);
    }
}
