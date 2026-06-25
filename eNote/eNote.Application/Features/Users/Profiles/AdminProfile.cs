namespace eNote.Application.Features.Users.Profiles;

public record AdminProfile(string? FirstName, string? LastName) : IUserProfile;
