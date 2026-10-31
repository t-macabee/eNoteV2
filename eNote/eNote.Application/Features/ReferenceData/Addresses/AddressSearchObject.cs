using eNote.Application.Common.Search;

namespace eNote.Application.Features.ReferenceData.Addresses;

public sealed class AddressSearchObject : BaseSearchObject
{
    public string? City { get; set; }
    public string? Street { get; set; }
}