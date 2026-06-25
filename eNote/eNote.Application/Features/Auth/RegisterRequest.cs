using System.ComponentModel.DataAnnotations;

namespace eNote.Application.Features.Auth;

public class RegisterRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    [Required]
    public string Password { get; set; } = string.Empty;

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}
