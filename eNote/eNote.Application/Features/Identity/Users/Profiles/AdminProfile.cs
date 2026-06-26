namespace eNote.Application.Features.Identity.Users.Profiles;

public record AdminProfile(string? FirstName, string? LastName) : IUserProfile;
