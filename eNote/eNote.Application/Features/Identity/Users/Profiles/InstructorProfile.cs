namespace eNote.Application.Features.Identity.Users.Profiles;

public record InstructorProfile(int Id, string? FirstName, string? LastName) : IUserProfile;
