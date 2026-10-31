namespace eNote.Domain.Entities.Shared;

public class City : BaseEntity
{
    public const int NameMaxLength = 100;

    public string Name { get; set; } = null!;

    private readonly List<Address> _addresses = [];
    public IReadOnlyCollection<Address> Addresses => _addresses;
}
