using System.Text.Json.Serialization;

namespace eNote.API;

public sealed record ErrorResponse
{
    public int Status { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IDictionary<string, string[]>? Errors { get; init; }
}
