namespace eNote.Application.Features.Identity.Users;

public static class UserNameHelper
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

    public static string? FormatName(UserIdentityDto? user)
    {
        if (user is null)
        {
            return null;
        }

        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? user.Username : fullName;
    }

    private static bool Contains(string? value, string name) =>
        value?.Contains(name, StringComparison.OrdinalIgnoreCase) == true;
}
