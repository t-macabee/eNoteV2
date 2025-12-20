using eNote.Application.Models.Shared;

namespace eNote.Application.DTOs.Users
{
    public record UserIdentityDto(int Id, string Username, string? FirstName, string? LastName, DateTime? DateOfBirth, AddressDto? Address, bool IsActive);
}
