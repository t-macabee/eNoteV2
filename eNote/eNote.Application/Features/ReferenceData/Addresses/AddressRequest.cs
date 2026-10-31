namespace eNote.Application.Features.ReferenceData.Addresses;

public sealed class AddressRequest
{
    public int CityId { get; set; }
    public string Street { get; set; } = null!;
    public string Number { get; set; } = null!;
}
