namespace eNote.Application.DTOs.Profile
{
    public record InstructorProfile(int Id, string? FirstName, string? LastName) : IUserProfile;
}
