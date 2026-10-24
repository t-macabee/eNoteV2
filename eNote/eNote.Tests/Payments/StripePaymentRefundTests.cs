using eNote.Domain.Entities.Shared;

namespace eNote.Tests.Payments;

public sealed class StripePaymentRefundTests
{
    private static readonly DateTime Now = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    public static TheoryData<string> PaymentTypes => ["RentalPayment", "CoursePayment"];

    [Theory]
    [MemberData(nameof(PaymentTypes))]
    public void ApplyRefund_Partial_LeavesPartiallyRefunded(string paymentType)
    {
        var payment = CreatePayment(paymentType);

        payment.ApplyRefund(2000, "re_test_1", Now);

        Assert.Equal(PaymentStatus.PartiallyRefunded, payment.Status);
        Assert.Equal(2000, payment.RefundedCents);
        Assert.Equal("re_test_1", payment.StripeRefundId);
        Assert.Equal(Now, payment.RefundedAt);
    }

    [Theory]
    [MemberData(nameof(PaymentTypes))]
    public void ApplyRefund_SecondPartial_AccumulatesToRefunded(string paymentType)
    {
        var payment = CreatePayment(paymentType);

        payment.ApplyRefund(2000, "re_test_1", Now);
        payment.ApplyRefund(3000, "re_test_2", Now);

        Assert.Equal(PaymentStatus.Refunded, payment.Status);
        Assert.Equal(5000, payment.RefundedCents);
        Assert.Equal(2, payment.Refunds.Count);
    }

    [Theory]
    [MemberData(nameof(PaymentTypes))]
    public void ApplyRefund_WholeAmount_SetsRefunded(string paymentType)
    {
        var payment = CreatePayment(paymentType);

        payment.ApplyRefund(5000, "re_test_1", Now);

        Assert.Equal(PaymentStatus.Refunded, payment.Status);
        Assert.Equal(0, payment.AmountChargedCents - payment.RefundedCents);
    }

    [Theory]
    [MemberData(nameof(PaymentTypes))]
    public void ApplyRefund_RepeatedRefundId_CountsOnce(string paymentType)
    {
        var payment = CreatePayment(paymentType);

        payment.ApplyRefund(2000, "re_test_1", Now);
        payment.ApplyRefund(2000, "re_test_1", Now);

        Assert.Equal(2000, payment.RefundedCents);
        Assert.Single(payment.Refunds);
    }

    [Theory]
    [MemberData(nameof(PaymentTypes))]
    public void ReverseRefund_FailedRefund_ReturnsToSucceeded(string paymentType)
    {
        var payment = CreatePayment(paymentType);
        payment.ApplyRefund(2000, "re_test_1", Now);

        payment.ReverseRefund(2000, "re_test_1");

        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        Assert.Null(payment.RefundedCents);
        Assert.Null(payment.StripeRefundId);
        Assert.Null(payment.RefundedAt);
        Assert.Empty(payment.Refunds);
    }

    [Theory]
    [MemberData(nameof(PaymentTypes))]
    public void ReverseRefund_OneOfTwoRefunds_LeavesPartiallyRefunded(string paymentType)
    {
        var payment = CreatePayment(paymentType);
        payment.ApplyRefund(2000, "re_test_1", Now);
        payment.ApplyRefund(1000, "re_test_2", Now);

        payment.ReverseRefund(2000, "re_test_1");

        Assert.Equal(PaymentStatus.PartiallyRefunded, payment.Status);
        Assert.Equal(1000, payment.RefundedCents);
    }

    [Fact]
    public void Refund_Row_IsLinkedToRentalPayment_ForRentalPayment()
    {
        var payment = new RentalPayment(1, 1, "pi_test_1", 5000, "eur", PaymentStatus.Succeeded);

        payment.ApplyRefund(2000, "re_test_1", Now);

        var refund = Assert.Single(payment.Refunds);
        Assert.Same(payment, refund.RentalPayment);
        Assert.Null(refund.CoursePayment);
        Assert.Equal("re_test_1", refund.StripeRefundId);
        Assert.Equal(2000, refund.AmountCents);
    }

    [Fact]
    public void Refund_Row_IsLinkedToCoursePayment_ForCoursePayment()
    {
        var payment = new CoursePayment(1, "pi_test_1", 5000, "bam", PaymentStatus.Succeeded);

        payment.ApplyRefund(2000, "re_test_1", Now);

        var refund = Assert.Single(payment.Refunds);
        Assert.Same(payment, refund.CoursePayment);
        Assert.Null(refund.RentalPayment);
        Assert.Equal("re_test_1", refund.StripeRefundId);
        Assert.Equal(2000, refund.AmountCents);
    }

    [Theory]
    [MemberData(nameof(PaymentTypes))]
    public void MarkCanceled_SetsCanceledStatus(string paymentType)
    {
        var payment = CreatePayment(paymentType);

        payment.MarkCanceled();

        Assert.Equal(PaymentStatus.Canceled, payment.Status);
    }

    private static StripePayment CreatePayment(string paymentType) => paymentType switch
    {
        "RentalPayment" => new RentalPayment(1, 1, "pi_test_1", 5000, "eur", PaymentStatus.Succeeded),
        "CoursePayment" => new CoursePayment(1, "pi_test_1", 5000, "bam", PaymentStatus.Succeeded),
        _ => throw new ArgumentOutOfRangeException(nameof(paymentType), paymentType, "Unknown payment type.")
    };
}
