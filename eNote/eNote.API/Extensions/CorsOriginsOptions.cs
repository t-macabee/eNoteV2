namespace eNote.API.Extensions;

public sealed class CorsOriginsOptions
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; set; } = [];
}
