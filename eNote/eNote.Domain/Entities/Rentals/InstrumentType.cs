namespace eNote.Domain.Entities.Rentals;

public class InstrumentType : BaseEntity
{
    public const int TypeMaxLength = 100;

    private readonly List<Instrument> _instruments = [];

    public string Type { get; set; } = null!;
    public decimal MonthlyFee { get; set; }

    public IReadOnlyCollection<Instrument> Instruments => _instruments;
}
