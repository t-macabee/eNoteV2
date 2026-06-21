namespace eNote.Application.Features.ReferenceData.MusicStores;

public sealed class MusicStoreRequest
{
    public string StoreName { get; set; } = null!;
    public string BusinessHours { get; set; } = null!;
}
