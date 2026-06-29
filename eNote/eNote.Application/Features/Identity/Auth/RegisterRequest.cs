using System.ComponentModel.DataAnnotations;

namespace eNote.Application.Features.Identity.Auth;

public class RegisterRequest
{
    public required string Username { get; set; }
    [EmailAddress]
    public required string Email { get; set; }
    public required string Password { get; set; }

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}
