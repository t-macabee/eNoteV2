using System.ComponentModel.DataAnnotations;

namespace eNote.Infrastructure.Identity;

public sealed record JwtOptions
{
    public const string SectionName = "Jwt";

    [Required(ErrorMessage = "Jwt__Key is required.")]
    [MinLength(32, ErrorMessage = "Jwt__Key must be at least 32 characters.")]
    public string Key { get; init; } = string.Empty;

    [Required(ErrorMessage = "Jwt__Issuer is required.")]
    public string Issuer { get; init; } = string.Empty;

    [Required(ErrorMessage = "Jwt__Audience is required.")]
    public string Audience { get; init; } = string.Empty;

    [Range(5, 120)]
    public int ExpirationMinutes { get; init; }
}
