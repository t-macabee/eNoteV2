using eNote.Application.Features.Identity.Users.Profiles;

namespace eNote.Application.Features.Identity.Users;

public sealed record UserProfileResponse(string Role, IUserProfile Profile);
