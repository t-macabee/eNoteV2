using eNote.Model.Profiles;

namespace eNote.Model.Auth
{
    public sealed record UserProfileResponse(string ProfileType, IUserProfile Profile);
}
