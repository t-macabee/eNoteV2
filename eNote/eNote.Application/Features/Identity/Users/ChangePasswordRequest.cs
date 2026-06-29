using System.ComponentModel.DataAnnotations;

namespace eNote.Application.Features.Identity.Users;

public class ChangePasswordRequest
{
    public required string CurrentPassword { get; set; }
    public required string NewPassword { get; set; }

    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    public required string ConfirmNewPassword { get; set; }
}
