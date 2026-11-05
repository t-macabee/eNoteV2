namespace eNote.Domain.Entities.Rentals;

public class InstrumentType : BaseEntity
{
    public const int TypeMaxLength = 100;
    public const decimal MinMonthlyFee = 30m;
    public const decimal MaxMonthlyFee = 99_999_999.99m;

    private readonly List<Instrument> _instruments = [];

    public string Type { get; set; } = null!;
    public decimal MonthlyFee { get; set; }

    public IReadOnlyCollection<Instrument> Instruments => _instruments;
}
