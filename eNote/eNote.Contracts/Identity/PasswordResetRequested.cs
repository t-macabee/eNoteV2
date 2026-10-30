namespace eNote.Contracts.Identity;

public sealed record PasswordResetRequested(int UserId, DateTime OccurredAtUtc);
