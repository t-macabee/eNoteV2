using eNote.Contracts.DTOs.Common;

namespace eNote.Contracts.DTOs.Profiles
{
    public record StudentProfile(int Id, DateTime EnrollmentDate, string? FirstName, string? LastName, DateTime? DateOfBirth, Address? Address) : IUserProfile;    
}
