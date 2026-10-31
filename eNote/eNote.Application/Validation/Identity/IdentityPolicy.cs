namespace eNote.Application.Validation.Identity;

/// <summary>
/// This class holds the shared password and lockout policy values.
/// </summary>
public static class IdentityPolicy
{
    public const int PasswordMinLength = 8;
    public const int MaxFailedAccessAttempts = 5;
    public const int LockoutMinutes = 15;
    public const int UserNameMaxLength = 256;
    public const int EmailMaxLength = 256;
    public const int PersonNameMaxLength = 256;
}
