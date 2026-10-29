namespace eNote.Tests.Domain;

public sealed class StudentMembershipTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void HasActiveMembership_ReturnsTrue_WhenPaidUntilIsTodayOrLater()
    {
        Student student = new(1, Now);
        student.UpdateMembership(Now.Date);

        Assert.True(student.HasActiveMembership(Now));
    }

    [Fact]
    public void HasActiveMembership_ReturnsFalse_WhenPaidUntilIsNullOrExpired()
    {
        Student student = new(1, Now);

        Assert.False(student.HasActiveMembership(Now));

        student.UpdateMembership(Now.Date.AddDays(-1));

        Assert.False(student.HasActiveMembership(Now));
    }
}
