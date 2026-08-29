namespace eNote.Domain.Entities.Shared;

public class City : BaseEntity
{
    public string Name { get; set; } = null!;

    private readonly List<Address> _addresses = [];
    public IReadOnlyCollection<Address> Addresses => _addresses;
}
