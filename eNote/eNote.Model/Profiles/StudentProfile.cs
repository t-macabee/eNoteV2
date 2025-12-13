using eNote.Model.Shared;

namespace eNote.Model.Profiles
{
    public record StudentProfile(int Id, DateTime EnrollmentDate, string? FirstName, string? LastName, DateTime? DateOfBirth, AddressDto? Address) : IUserProfile;    
}
