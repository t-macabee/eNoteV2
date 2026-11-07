using eNote.Application.Validation.Identity;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace eNote.Infrastructure.Identity;

public class AppUser : IdentityUser<int>, IEntity
{
    [Required]
    [MaxLength(IdentityPolicy.PersonNameMaxLength)]
    public string FirstName { get; set; } = null!;
    [Required]
    [MaxLength(IdentityPolicy.PersonNameMaxLength)]
    public string LastName { get; set; } = null!;
    public DateTime? DateOfBirth { get; set; }
    [MaxLength(512)]
    public string? PicturePath { get; set; }
    public bool IsActive { get; set; }
}
