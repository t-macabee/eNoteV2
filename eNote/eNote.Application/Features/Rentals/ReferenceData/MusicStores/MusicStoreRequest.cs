namespace eNote.Application.Features.Rentals.ReferenceData.MusicStores;

public sealed class MusicStoreRequest
{
    public string StoreName { get; set; } = null!;
    public string BusinessHours { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public int? AddressId { get; set; }
}
