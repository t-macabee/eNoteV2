namespace eNote.Contracts.Identity;

public sealed record MembershipWarning(
    int StudentUserId,
    string Title,
    string Body,
    DateTime OccurredAtUtc);
