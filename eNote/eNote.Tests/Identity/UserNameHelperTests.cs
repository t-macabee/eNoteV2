using eNote.Application.Features.Identity.Users;

namespace eNote.Tests.Identity;

public sealed class UserNameHelperTests
{

    [Fact]
    public void FormatName_PrefersFullName()
    {
        var user = new UserIdentityDto { Username = "jdoe", FirstName = "Jane", LastName = "Doe" };

        Assert.Equal("Jane Doe", UserNameHelper.FormatName(user));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    public void FormatName_FallsBackToUsername_WhenNameMissing(string? firstName, string? lastName)
    {
        var user = new UserIdentityDto { Username = "jdoe", FirstName = firstName, LastName = lastName };

        Assert.Equal("jdoe", UserNameHelper.FormatName(user));
    }

    [Fact]
    public void FormatName_ReturnsNull_WhenUserMissing()
    {
        Assert.Null(UserNameHelper.FormatName(null));
    }
}
