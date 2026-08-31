namespace eNote.Domain.Entities.Rentals;

public class MusicStore : AuditableEntity
{

    private readonly List<MusicStoreEmployee> _employees = [];

    private readonly List<Instrument> _instruments = [];

    public string StoreName { get; private set; } = null!;
    public string BusinessHours { get; private set; } = null!;

    public int? AddressId { get; private set; }
    public Address? Address { get; private set; }

    public IReadOnlyCollection<MusicStoreEmployee> Employees => _employees;
    public IReadOnlyCollection<Instrument> Instruments => _instruments;

    protected MusicStore()
    {
    }

    public MusicStore(string storeName, string businessHours, int? addressId = null)
    {
        StoreName = storeName;
        BusinessHours = businessHours;
        AddressId = addressId;
    }

    public void UpdateDetails(string storeName, string businessHours, int? addressId = null)
    {
        StoreName = storeName;
        BusinessHours = businessHours;
        AddressId = addressId;
    }
}
