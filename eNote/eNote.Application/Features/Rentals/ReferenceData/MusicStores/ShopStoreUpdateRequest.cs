namespace eNote.Application.Features.Rentals.ReferenceData.MusicStores;

public sealed class ShopStoreUpdateRequest
{
    public string BusinessHours { get; set; } = null!;
    public string? PhoneNumber { get; set; }
}
