using eNote.Application.DTOs.Shared;

namespace eNote.Application.DTOs.Profile
{
    public record MusicShopProfile(int Id, string StoreName, string BusinessHours, AddressDto? Address) : IUserProfile;
}
