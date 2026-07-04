using eNote.Domain.Entities.Shared.Base;
namespace eNote.Domain.Entities.Rentals;

public class InstrumentType : BaseEntity
{
    // ReSharper disable once CollectionNeverUpdated.Local — EF Core backing field
    private readonly List<Instrument> _instruments = [];

    public string Type { get; set; } = null!;
    public decimal MonthlyFee { get; set; }

    public IReadOnlyCollection<Instrument> Instruments => _instruments;
}
