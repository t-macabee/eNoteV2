using System.ComponentModel.DataAnnotations;

namespace eNote.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMQ";

    [Required(ErrorMessage = "RabbitMQ__Host is required.")]
    public string Host { get; set; } = string.Empty;

    [Required(ErrorMessage = "RabbitMQ__VirtualHost is required.")]
    public string VirtualHost { get; set; } = string.Empty;

    [Required(ErrorMessage = "RabbitMQ__User is required.")]
    public string User { get; set; } = string.Empty;

    [Required(ErrorMessage = "RabbitMQ__Password is required.")]
    public string Password { get; set; } = string.Empty;
}
