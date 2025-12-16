using eNote.Application.Models.Profile;

namespace eNote.Application.Models.Auth
{
    public sealed record UserProfileResponse(string ProfileType, IUserProfile Profile);
}
