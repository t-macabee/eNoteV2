using eNote.Contracts.DTOs.Profiles;

namespace eNote.Contracts.DTOs.Auth
{
    public sealed record UserProfileResult(string ProfileType, IUserProfile Profile);
}
