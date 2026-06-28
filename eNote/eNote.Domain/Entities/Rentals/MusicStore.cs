using eNote.Domain.Entities;

namespace eNote.Domain.Entities;

public class MusicStore : AuditableEntity
{
    private readonly List<MusicStoreEmployee> _employees = [];
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
