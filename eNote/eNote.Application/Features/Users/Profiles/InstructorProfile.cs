namespace eNote.Application.Features.Users.Profiles
{
    public record InstructorProfile(int Id, string? FirstName, string? LastName) : IUserProfile;
}
