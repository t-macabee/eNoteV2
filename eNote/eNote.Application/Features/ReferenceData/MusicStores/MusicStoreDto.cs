namespace eNote.Application.Features.ReferenceData.MusicStores;

public sealed class MusicStoreDto
{
    public int Id { get; init; }
    public string StoreName { get; init; } = null!;
    public string BusinessHours { get; init; } = null!;
}
