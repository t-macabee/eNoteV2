namespace eNote.Application.Validation.Identity;

/// <summary>
/// This class holds the shared password and lockout policy values.
/// </summary>
public static class IdentityPolicy
{
    public const int PasswordMinLength = 8;
    public const int MaxFailedAccessAttempts = 5;
    public const int LockoutMinutes = 15;
}
