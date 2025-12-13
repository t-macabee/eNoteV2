using eNote.Model.Shared;

namespace eNote.Model.Profiles
{
    public record MusicShopProfile(int Id, string StoreName, string BusinessHours, AddressDto? Address) : IUserProfile;
}
