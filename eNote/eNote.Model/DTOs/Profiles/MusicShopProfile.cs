using eNote.Contracts.DTOs.Common;

namespace eNote.Contracts.DTOs.Profiles
{
    public record MusicShopProfile(int Id, string StoreName, string BusinessHours, Address? Address) : IUserProfile;
}
