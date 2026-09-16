using eNote.Application.Features.Identity.Users;

namespace eNote.Tests.Identity;

public sealed class UserNameHelperTests
{
    [Theory]
    [InlineData("Jane", "Doe", "jdoe", "Jane", true)]
    [InlineData("Jane", "Doe", "jdoe", "doe", true)]
    [InlineData("Jane", "Doe", "jdoe", "JDOE", true)]
    [InlineData("Jane", "Doe", "jdoe", "jane doe", true)]
    [InlineData("Jane", "Doe", "jdoe", "Smith", false)]
    [InlineData(null, null, null, "Smith", false)]
    public void MatchesName_MatchesFirstNameLastNameUsernameOrFullName(string? firstName, string? lastName, string? username, string? name, bool expected)
    {
        Assert.Equal(expected, UserNameHelper.MatchesName(firstName, lastName, username, name));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MatchesName_ReturnsTrue_WhenNameBlankOrMissing(string? name)
    {
        Assert.True(UserNameHelper.MatchesName("Jane", "Doe", "jdoe", name));
    }

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
