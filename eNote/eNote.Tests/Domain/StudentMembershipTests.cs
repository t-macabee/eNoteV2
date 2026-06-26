using eNote.Domain.Entities.Identity;
using Xunit;

namespace eNote.Tests.Domain;

public sealed class StudentMembershipTests
{
    [Fact]
    public void HasActiveMembership_ReturnsTrue_WhenPaidUntilIsTodayOrLater()
    {
        Student student = new(1, DateTime.UtcNow);
        student.UpdateMembership(DateTime.UtcNow.Date);

        Assert.True(student.HasActiveMembership(DateTime.UtcNow));
    }

    [Fact]
    public void HasActiveMembership_ReturnsFalse_WhenPaidUntilIsNullOrExpired()
    {
        Student student = new(1, DateTime.UtcNow);

        Assert.False(student.HasActiveMembership(DateTime.UtcNow));

        student.UpdateMembership(DateTime.UtcNow.Date.AddDays(-1));

        Assert.False(student.HasActiveMembership(DateTime.UtcNow));
    }
}
