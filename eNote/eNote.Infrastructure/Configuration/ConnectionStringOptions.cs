using System.ComponentModel.DataAnnotations;

namespace eNote.Infrastructure.Configuration;

public sealed class ConnectionStringOptions
{
    public const string SectionName = "ConnectionStrings";

    [Required(ErrorMessage = "ConnectionStrings__DefaultConnection is required.")]
    public string DefaultConnection { get; set; } = string.Empty;
}
