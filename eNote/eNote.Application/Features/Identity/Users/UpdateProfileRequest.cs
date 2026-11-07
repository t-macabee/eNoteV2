namespace eNote.Application.Features.Identity.Users;

public class UpdateProfileRequest
{
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public DateTime? DateOfBirth { get; set; }
}
