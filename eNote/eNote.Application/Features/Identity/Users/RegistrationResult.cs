namespace eNote.Application.Features.Identity.Users;

public sealed record RegistrationResult(int UserId, string Username, IReadOnlyList<string> Roles);
