namespace eNote.Tests.TestUtils;

public sealed class TestUserNameHelperTests
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
        Assert.Equal(expected, TestUserNameHelper.MatchesName(firstName, lastName, username, name));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MatchesName_ReturnsTrue_WhenNameBlankOrMissing(string? name)
    {
        Assert.True(TestUserNameHelper.MatchesName("Jane", "Doe", "jdoe", name));
    }
}
