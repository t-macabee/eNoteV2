namespace eNote.Application.Features.Identity.Users;

public sealed class DelegatedUserCreateRequest
{
    public required string Username { get; init; }
    public required string Email { get; init; }
    public required string Password { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
}
