using eNote.Application.Features.Users.Profiles;

namespace eNote.Application.Features.Users.DTOs
{
    public sealed record UserProfileResponse(string Role, IUserProfile Profile);
}
