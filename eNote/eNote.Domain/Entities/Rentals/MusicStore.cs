using eNote.Domain.Entities.Shared.Base;
using eNote.Domain.Entities.Identity;
namespace eNote.Domain.Entities.Rentals;

public class MusicStore : AuditableEntity
{
    // ReSharper disable once CollectionNeverUpdated.Local — EF Core backing field
    private readonly List<MusicStoreEmployee> _employees = [];
    // ReSharper disable once CollectionNeverUpdated.Local — EF Core backing field
    private readonly List<Instrument> _instruments = [];

    public string StoreName { get; private set; } = null!;
    public string BusinessHours { get; private set; } = null!;

    public IReadOnlyCollection<MusicStoreEmployee> Employees => _employees;
    public IReadOnlyCollection<Instrument> Instruments => _instruments;

    protected MusicStore()
    {
    }

    public MusicStore(string storeName, string businessHours)
    {
        StoreName = storeName;
        BusinessHours = businessHours;
    }

    public void UpdateDetails(string storeName, string businessHours)
    {
        StoreName = storeName;
        BusinessHours = businessHours;
    }
}
