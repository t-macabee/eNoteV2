using eNote.Application.DTOs.Shared;

namespace eNote.Application.DTOs.Profiles
{
    public record MusicShopProfile(int Id, string StoreName, string BusinessHours, AddressDto? Address) : IUserProfile;
}
