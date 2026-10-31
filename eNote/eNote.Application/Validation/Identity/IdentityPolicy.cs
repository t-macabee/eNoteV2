namespace eNote.Application.Validation.Identity;

public static class IdentityPolicy
{
    public const int PasswordMinLength = 8;
    public const int MaxFailedAccessAttempts = 5;
    public const int LockoutMinutes = 15;
    public const int UserNameMaxLength = 256;
    public const int EmailMaxLength = 256;
    public const int PersonNameMaxLength = 256;
}
