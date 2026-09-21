namespace eNote.Application.Features.Identity.Users;

public static class UserNameHelper
{
    public static string? FormatName(UserIdentityDto? user)
    {
        if (user is null)
        {
            return null;
        }

        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? user.Username : fullName;
    }
}
