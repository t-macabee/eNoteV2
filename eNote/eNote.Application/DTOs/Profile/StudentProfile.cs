using eNote.Application.DTOs.Shared;

namespace eNote.Application.DTOs.Profile
{
    public record StudentProfile(int Id, DateTime EnrollmentDate, string? FirstName, string? LastName, DateTime? DateOfBirth, AddressDto? Address) : IUserProfile;
}
