namespace eNote.Tests.Domain;

public sealed class EnrollmentTests
{
    private static readonly DateTime Now = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ExtendPaidUntil_FromNull_SetsPaidUntilFromUtcNow()
    {
        var enrollment = new Enrollment(1, 1, EnrollmentStatus.Active);

        var (_, end) = enrollment.ExtendPaidUntil(Now, 30);

        Assert.Equal(Now.AddDays(30), enrollment.PaidUntil);
        Assert.Equal(enrollment.PaidUntil, end);
    }

    [Fact]
    public void ExtendPaidUntil_FromPast_SetsPaidUntilFromUtcNow()
    {
        var enrollment = new Enrollment(1, 1, EnrollmentStatus.Active);
        enrollment.ExtendPaidUntil(Now.AddDays(-60), 30);

        enrollment.ExtendPaidUntil(Now, 30);

        Assert.Equal(Now.AddDays(30), enrollment.PaidUntil);
    }

    [Fact]
    public void ExtendPaidUntil_FromFuture_PreservesRemainingDays()
    {
        var enrollment = new Enrollment(1, 1, EnrollmentStatus.Active);
        enrollment.ExtendPaidUntil(Now, 30);

        var renewalTime = Now.AddDays(10);
        enrollment.ExtendPaidUntil(renewalTime, 30);

        Assert.Equal(Now.AddDays(60), enrollment.PaidUntil);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ExtendPaidUntil_NonPositiveDays_ThrowsArgumentOutOfRangeException(int days)
    {
        var enrollment = new Enrollment(1, 1, EnrollmentStatus.Active);

        Assert.Throws<ArgumentOutOfRangeException>(() => enrollment.ExtendPaidUntil(Now, days));
    }

    [Fact]
    public void RevokeRefundedAccessPeriod_FullRefundOfTheCurrentPeriod_MovesPaidUntilBack()
    {
        var enrollment = new Enrollment(1, 1, EnrollmentStatus.Active);
        var (periodStart, periodEnd) = enrollment.ExtendPaidUntil(Now, 30);
        var payment = CreateSucceededPayment(periodStart, periodEnd);

        payment.ApplyRefund(5000, "re_test_1", Now);
        enrollment.RevokeRefundedAccessPeriod(payment);

        Assert.Equal(PaymentStatus.Refunded, payment.Status);
        Assert.Equal(periodStart, enrollment.PaidUntil);
    }

    [Fact]
    public void RevokeRefundedAccessPeriod_PartialRefund_KeepsPaidUntil()
    {
        var enrollment = new Enrollment(1, 1, EnrollmentStatus.Active);
        var (periodStart, periodEnd) = enrollment.ExtendPaidUntil(Now, 30);
        var payment = CreateSucceededPayment(periodStart, periodEnd);

        payment.ApplyRefund(2000, "re_test_1", Now);
        enrollment.RevokeRefundedAccessPeriod(payment);

        Assert.Equal(PaymentStatus.PartiallyRefunded, payment.Status);
        Assert.Equal(periodEnd, enrollment.PaidUntil);
    }

    [Fact]
    public void RevokeRefundedAccessPeriod_PaymentWithoutPeriod_KeepsPaidUntil()
    {
        var enrollment = new Enrollment(1, 1, EnrollmentStatus.Active);
        var (_, periodEnd) = enrollment.ExtendPaidUntil(Now, 30);
        var payment = CreateSucceededPayment(null, null);

        payment.ApplyRefund(5000, "re_test_1", Now);
        enrollment.RevokeRefundedAccessPeriod(payment);

        Assert.Equal(periodEnd, enrollment.PaidUntil);
    }

    [Fact]
    public void RestoreRefundedAccessPeriod_ReversedFullRefund_RestoresPaidUntil()
    {
        var enrollment = new Enrollment(1, 1, EnrollmentStatus.Active);
        var (periodStart, periodEnd) = enrollment.ExtendPaidUntil(Now, 30);
        var payment = CreateSucceededPayment(periodStart, periodEnd);
        payment.ApplyRefund(5000, "re_test_1", Now);
        enrollment.RevokeRefundedAccessPeriod(payment);

        payment.ReverseRefund(5000, "re_test_1");
        enrollment.RestoreRefundedAccessPeriod(payment);

        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        Assert.Equal(periodEnd, enrollment.PaidUntil);
    }

    private static CoursePayment CreateSucceededPayment(DateTime? periodStart, DateTime? periodEnd)
    {
        var payment = new CoursePayment(1, "pi_test_1", 5000, "bam", PaymentStatus.RequiresAction);
        payment.MarkSucceeded("ch_test_1", "evt_test_1", Now, periodStart, periodEnd);
        return payment;
    }
}
