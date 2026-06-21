namespace eNote.Application.Features.ReferenceData.Addresses;

public sealed class AddressReferenceDto
{
    public int Id { get; init; }
    public string City { get; init; } = null!;
    public string Street { get; init; } = null!;
    public string Number { get; init; } = null!;
}
