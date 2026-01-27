using System.Text.Json.Serialization;

namespace eNote.Application.Features.Auth.DTOs
{
    public sealed class AuthResponse
    {
        [JsonPropertyName("userId")]
        public int UserId { get; init; }

        [JsonPropertyName("username")]
        public string Username { get; init; } = null!;

        [JsonPropertyName("roles")]
        public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();

        [JsonPropertyName("token")]
        public string Token { get; init; } = null!;
    }
}
