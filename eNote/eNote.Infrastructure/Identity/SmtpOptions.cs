using System.ComponentModel.DataAnnotations;

namespace eNote.Infrastructure.Identity;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    [Required(ErrorMessage = "Smtp__Host is required.")]
    public string Host { get; set; } = string.Empty;

    [Range(1, 65535, ErrorMessage = "Smtp__Port must be between 1 and 65535.")]
    public int Port { get; set; }

    [Required(ErrorMessage = "Smtp__EnableSsl is required.")]
    public bool? EnableSsl { get; set; }

    [Required(ErrorMessage = "Smtp__From is required.")]
    public string From { get; set; } = string.Empty;

    [Required(ErrorMessage = "Smtp__PasswordResetUrl is required.")]
    public string PasswordResetUrl { get; set; } = string.Empty;

    public string? Username { get; set; }

    public string? Password { get; set; }
}
