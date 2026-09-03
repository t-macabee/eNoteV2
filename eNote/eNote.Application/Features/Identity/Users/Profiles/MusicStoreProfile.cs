namespace eNote.Application.Features.Identity.Users.Profiles;

public record MusicStoreProfile(int Id, string StoreName, string BusinessHours, bool IsManager = false) : IUserProfile;
