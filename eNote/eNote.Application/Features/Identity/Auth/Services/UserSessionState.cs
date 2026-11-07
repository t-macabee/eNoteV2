namespace eNote.Application.Features.Identity.Auth.Services;

public sealed record UserSessionState(bool IsActive, string? SecurityStamp);
