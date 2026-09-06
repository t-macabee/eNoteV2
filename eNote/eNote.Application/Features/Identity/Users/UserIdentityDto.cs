namespace eNote.Application.Features.Identity.Users;

public class UserIdentityDto
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    public DateTime? DateOfBirth { get; set; }
    public bool HasPicture { get; set; }

    public bool IsActive { get; set; }
}
