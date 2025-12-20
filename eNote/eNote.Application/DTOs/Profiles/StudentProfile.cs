using eNote.Application.DTOs.Profiles;
using eNote.Application.Models.Shared;

namespace eNote.Application.Models.Profile
{
    public record StudentProfile(int Id, DateTime EnrollmentDate, string? FirstName, string? LastName, DateTime? DateOfBirth, AddressDto? Address) : IUserProfile;
}
