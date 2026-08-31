namespace eNote.Application.Features.Rentals.ReferenceData.MusicStores;

public sealed class MusicStoreDto
{
    public int Id { get; init; }
    public string StoreName { get; init; } = null!;
    public string BusinessHours { get; init; } = null!;
    public int? AddressId { get; init; }
    public string? AddressStreet { get; init; }
    public string? AddressCity { get; init; }
}
