using eNote.Infrastructure.Data.Entities;
using Microsoft.AspNetCore.Identity;

namespace eNote.Infrastructure.Identity
{
    public class AppUser : IdentityUser<int>
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public byte[]? Picture { get; set; }
        public bool Status { get; set; }

        public int? AddressId { get; set; }
        public Address? Address { get; set; }
    }
}
