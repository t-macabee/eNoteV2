using eNote.Application.Features.Identity.Users;

namespace eNote.Application.Features.Identity.Users.Profiles;

public record StudentProfile(int Id, DateTime EnrollmentDate, string? FirstName, string? LastName, DateTime? DateOfBirth, UserAddressDto? Address, DateTime? MembershipPaidUntil) : IUserProfile;
