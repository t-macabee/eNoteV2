namespace eNote.Application.DTOs.Profiles
{
    public sealed record UserProfileResponse(string Role, IUserProfile Profile);
}
