using System.Text.Json.Serialization;

namespace eNote.Application.DTOs.Auth
{
    public sealed class AuthResponse
    {
        [JsonPropertyName("user_id")] 
        public int UserId { get; init; }

        [JsonPropertyName("username")]
        public string Username { get; init; } = null!;

        [JsonPropertyName("roles")] 
        public IReadOnlyList<string> Roles { get; init; } = new List<string>().AsReadOnly();

        [JsonPropertyName("status")]
        public bool Status { get; init; }

        [JsonPropertyName("token")]
        public string Token { get; init; } = null!;
    }
}
