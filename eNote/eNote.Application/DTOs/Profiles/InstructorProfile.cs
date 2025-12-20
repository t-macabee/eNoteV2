using eNote.Application.DTOs.Profiles;

namespace eNote.Application.Models.Profile
{
    public record InstructorProfile(int Id, string? FirstName, string? LastName) : IUserProfile;
}
