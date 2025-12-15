using eNote.Application.DTOs.Profiles;

namespace eNote.Application.DTOs.Auth
{
    public sealed record UserProfileResponse(string ProfileType, IUserProfile Profile);
}
