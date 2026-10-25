using System.ComponentModel.DataAnnotations;

namespace eNote.Infrastructure.Storage;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    [Required(ErrorMessage = "Storage__Root is required.")]
    public string Root { get; set; } = string.Empty;
}
