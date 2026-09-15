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
    public void HasPaidAccess_ReturnsFalse_WhenPaidUntilIsNull()
    {
        var enrollment = new Enrollment(1, 1, EnrollmentStatus.Active);

        Assert.False(enrollment.HasPaidAccess(Now));
    }

    [Fact]
    public void HasPaidAccess_ReturnsTrue_WhenPaidUntilIsInFuture()
    {
        var enrollment = new Enrollment(1, 1, EnrollmentStatus.Active);
        enrollment.ExtendPaidUntil(Now, 30);

        Assert.True(enrollment.HasPaidAccess(Now.AddDays(15)));
    }

    [Fact]
    public void HasPaidAccess_ReturnsTrue_OnExactBoundary()
    {
        var enrollment = new Enrollment(1, 1, EnrollmentStatus.Active);
        enrollment.ExtendPaidUntil(Now, 30);

        Assert.True(enrollment.HasPaidAccess(Now.AddDays(30)));
    }

    [Fact]
    public void HasPaidAccess_ReturnsFalse_WhenPastTimestamp()
    {
        var enrollment = new Enrollment(1, 1, EnrollmentStatus.Active);
        enrollment.ExtendPaidUntil(Now, 30);

        Assert.False(enrollment.HasPaidAccess(Now.AddDays(30).AddSeconds(1)));
    }
}
