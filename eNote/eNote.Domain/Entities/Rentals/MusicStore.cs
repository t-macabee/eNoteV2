namespace eNote.Domain.Entities.Rentals;

public class MusicStore : AuditableEntity
{
    public const int StoreNameMaxLength = 100;
    public const int BusinessHoursMaxLength = 50;
    public const int PhoneNumberMaxLength = 30;

    private readonly List<MusicStoreEmployee> _employees = [];

    private readonly List<Instrument> _instruments = [];

    public string StoreName { get; private set; } = null!;
    public string BusinessHours { get; private set; } = null!;
    public string? PhoneNumber { get; private set; }
    public string? ImagePath { get; private set; }

    public int? AddressId { get; private set; }
    public Address? Address { get; private set; }

    public IReadOnlyCollection<MusicStoreEmployee> Employees => _employees;
    public IReadOnlyCollection<Instrument> Instruments => _instruments;

    protected MusicStore()
    {
    }

    public MusicStore(string storeName, string businessHours, int? addressId = null, string? phoneNumber = null)
    {
        StoreName = storeName;
        BusinessHours = businessHours;
        AddressId = addressId;
        PhoneNumber = phoneNumber;
    }

    public void UpdateDetails(string storeName, string businessHours, int? addressId = null, string? phoneNumber = null)
    {
        StoreName = storeName;
        BusinessHours = businessHours;
        AddressId = addressId;
        PhoneNumber = phoneNumber;
    }

    public void UpdateImagePath(string path) => ImagePath = path;
}
