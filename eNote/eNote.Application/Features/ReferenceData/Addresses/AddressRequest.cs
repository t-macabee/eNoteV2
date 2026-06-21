namespace eNote.Application.Features.ReferenceData.Addresses;

public sealed class AddressRequest
{
    public string City { get; set; } = null!;
    public string Street { get; set; } = null!;
    public string Number { get; set; } = null!;
}
