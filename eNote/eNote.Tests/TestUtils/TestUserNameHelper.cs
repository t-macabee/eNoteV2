namespace eNote.Tests.TestUtils;

public static class TestUserNameHelper
{
    public static bool MatchesName(string? firstName, string? lastName, string? username, string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return true;
        }

        var fullName = $"{firstName} {lastName}".Trim();

        return Contains(firstName, name)
            || Contains(lastName, name)
            || Contains(username, name)
            || Contains(fullName, name);
    }

    private static bool Contains(string? value, string name) =>
        value?.Contains(name, StringComparison.OrdinalIgnoreCase) == true;
}
