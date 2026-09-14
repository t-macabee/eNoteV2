namespace eNote.Tests.Domain;

public sealed class CoursePaymentTests
{
    private static readonly DateTime Now = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Constructor_SetsProperties()
    {
        var payment = new CoursePayment(10, "pi_test_123", 5000, "bam", PaymentStatus.RequiresAction);

        Assert.Equal(10, payment.EnrollmentId);
        Assert.Equal("pi_test_123", payment.StripePaymentIntentId);
        Assert.Equal(5000, payment.AmountChargedCents);
        Assert.Equal("bam", payment.Currency);
        Assert.Equal(PaymentStatus.RequiresAction, payment.Status);
        Assert.Null(payment.StripeChargeId);
        Assert.Null(payment.StripeEventId);
        Assert.Null(payment.PaidAt);
        Assert.Null(payment.PeriodStart);
        Assert.Null(payment.PeriodEnd);
    }

    [Fact]
    public void MarkSucceeded_UpdatesFieldsAndStatus()
    {
        var payment = new CoursePayment(10, "pi_test_123", 5000, "bam", PaymentStatus.RequiresAction);
        var periodStart = Now;
        var periodEnd = Now.AddDays(30);

        payment.MarkSucceeded("ch_test_456", "evt_test_789", Now, periodStart, periodEnd);

        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        Assert.Equal("ch_test_456", payment.StripeChargeId);
        Assert.Equal("evt_test_789", payment.StripeEventId);
        Assert.Equal(Now, payment.PaidAt);
        Assert.Equal(periodStart, payment.PeriodStart);
        Assert.Equal(periodEnd, payment.PeriodEnd);
    }

    [Fact]
    public void MarkFailed_UpdatesStatusAndEventId()
    {
        var payment = new CoursePayment(10, "pi_test_123", 5000, "bam", PaymentStatus.RequiresAction);

        payment.MarkFailed("evt_test_failed_1");

        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Equal("evt_test_failed_1", payment.StripeEventId);
        Assert.Null(payment.StripeChargeId);
        Assert.Null(payment.PaidAt);
    }
}
