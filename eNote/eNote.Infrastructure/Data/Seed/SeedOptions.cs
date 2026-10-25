namespace eNote.Infrastructure.Data.Seed;

public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    public bool Enabled { get; set; }

    public string DefaultPassword { get; set; } = string.Empty;
}
