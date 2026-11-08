namespace eNote.Application.Features.Rentals.ReferenceData.Manufacturers;

public sealed class ManufacturerDto
{
    public int Id { get; init; }
    public string Name { get; init; } = null!;
    public string? DeleteBlockedReason { get; init; }
}
