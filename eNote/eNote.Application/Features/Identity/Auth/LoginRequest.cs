namespace eNote.Application.Features.Identity.Auth;

public class LoginRequest
{
    public string Username { get; set; } = null!;

    public string Password { get; set; } = null!;
}
