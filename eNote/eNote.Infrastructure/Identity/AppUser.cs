using eNote.Domain.Entities.Shared.Base;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace eNote.Infrastructure.Identity;

public class AppUser : IdentityUser<int>, IEntity
{
    [MaxLength(256)]
    public string? FirstName { get; set; }
    [MaxLength(256)]
    public string? LastName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    [MaxLength(512)]
    public string? PicturePath { get; set; }
    public bool IsActive { get; set; }
}
