namespace eNote.Application.Common.Localization;

public static class ErrorMapping
{
    public static bool IsConflict(string? error) =>
        error is Messages.UsernameTaken
            or Messages.EmailTaken
            or Messages.CannotModifyOwnAccount
            or Messages.UserDeleteBlocked;

    public static bool IsNotFound(string? error) => error == Messages.NotFound;
}
