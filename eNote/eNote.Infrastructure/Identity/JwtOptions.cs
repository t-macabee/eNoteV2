using System.ComponentModel.DataAnnotations;

namespace eNote.Infrastructure.Identity;

public sealed record JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Key { get; init; } = string.Empty;

    [Required]
    public string Issuer { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;

    [Range(5, 120)]
    public int ExpirationMinutes { get; init; } = 120;
}
