using eNote.Application.Features.Identity.Users.Profiles;

namespace eNote.Application.Features.Identity.Users;

public sealed record UserProfileResponse(string Role, string Username, string? Email, IUserProfile Profile);
