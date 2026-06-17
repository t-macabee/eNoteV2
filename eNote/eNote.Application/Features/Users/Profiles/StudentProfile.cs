namespace eNote.Application.Features.Users.Profiles
{
    public record StudentProfile(int Id, DateTime EnrollmentDate, string? FirstName, string? LastName, DateTime? DateOfBirth, AddressDto? Address, DateTime? MembershipPaidUntil) : IUserProfile;
}
