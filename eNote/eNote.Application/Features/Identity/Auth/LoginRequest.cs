using System.ComponentModel.DataAnnotations;

namespace eNote.Application.Features.Identity.Auth;

public class LoginRequest
{
    [Required(ErrorMessage = "Korisničko ime je obavezno.")]
    public string Username { get; set; } = null!;

    [Required(ErrorMessage = "Lozinka je obavezna.")]
    public string Password { get; set; } = null!;
}
