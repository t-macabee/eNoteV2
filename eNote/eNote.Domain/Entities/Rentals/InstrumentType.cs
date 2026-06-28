using eNote.Domain.Entities;

namespace eNote.Domain.Entities;

public class InstrumentType : BaseEntity
{
    private readonly List<Instrument> _instruments = [];

    public string Type { get; set; } = null!;
    public decimal MonthlyFee { get; set; }

    public IReadOnlyCollection<Instrument> Instruments => _instruments;
}
