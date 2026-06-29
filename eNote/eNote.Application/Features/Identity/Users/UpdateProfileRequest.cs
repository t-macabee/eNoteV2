using System.ComponentModel.DataAnnotations;

namespace eNote.Application.Features.Identity.Users;

public class UpdateProfileRequest
{
    [EmailAddress]
    public required string Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime? DateOfBirth { get; set; }
}
