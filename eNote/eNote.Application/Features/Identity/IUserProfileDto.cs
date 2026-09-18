namespace eNote.Application.Features.Identity;

public interface IUserProfileDto
{
    string? FirstName { get; }
    string? LastName { get; }
    string? Username { get; }
    bool IsActive { get; }
}
