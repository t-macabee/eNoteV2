namespace eNote.Application.Features.Identity.Users;

public class DelegatedUserCreateRequest
{
    public required string Username { get; init; }
    public required string Email { get; init; }
    public required string Password { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
}
