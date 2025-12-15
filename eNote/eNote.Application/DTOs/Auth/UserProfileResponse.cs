using eNote.Application.DTOs.Profile;

namespace eNote.Application.DTOs.Auth
{
    public sealed record UserProfileResponse(string ProfileType, IUserProfile Profile);
}
