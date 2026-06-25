using System.ComponentModel.DataAnnotations;

namespace eNote.Application.Features.Users;

public class UpdateProfileRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime? DateOfBirth { get; set; }
}
