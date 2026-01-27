using eNote.Application.Common.DTOs;

namespace eNote.Application.Features.Users.Profiles
{
    public record MusicShopProfile(int Id, string StoreName, string BusinessHours, AddressDto? Address) : IUserProfile;
}
