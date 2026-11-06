using System.Text.Json.Serialization;

namespace eNote.Application.Features.Identity.Users;

public sealed record UserStatusRequest([property: JsonRequired] bool IsActive);

