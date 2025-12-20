using eNote.Application.DTOs.Profiles;
using eNote.Application.Models.Shared;

namespace eNote.Application.Models.Profile
{
    public record MusicShopProfile(int Id, string StoreName, string BusinessHours, AddressDto? Address) : IUserProfile;
}
