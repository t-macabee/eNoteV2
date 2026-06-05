using eNote.Application.Features.Users.Profiles;

namespace eNote.Application.Features.Users
{
    public sealed record UserProfileResponse(string Role, IUserProfile Profile);
}
