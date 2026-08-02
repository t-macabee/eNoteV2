using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace eNote.Infrastructure.Identity;

public class AppUser : IdentityUser<int>
{
    [MaxLength(256)]
    public string? FirstName { get; set; }
    [MaxLength(256)]
    public string? LastName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    [MaxLength(512)]
    public string? PicturePath { get; set; }
    public bool IsActive { get; set; }

    public int? AddressId { get; init; }
    public Address? Address { get; init; }
}
