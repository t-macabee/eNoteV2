namespace eNote.Application.Features.Rentals.ReferenceData.Addresses;

public sealed class AddressReferenceDto
{
    public int Id { get; init; }
    public int CityId { get; init; }
    public string City { get; init; } = null!;
    public string Street { get; init; } = null!;
    public string Number { get; init; } = null!;
}
