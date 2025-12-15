namespace eNote.Application.DTOs.Profiles
{
    public record InstructorProfile(int Id, string? FirstName, string? LastName) : IUserProfile;    
}
