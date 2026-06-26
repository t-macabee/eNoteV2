using eNote.Domain.Entities.Shared.Base;

namespace eNote.Domain.Entities.Rentals;

public class InstrumentType : BaseEntity
{
    public string Type { get; set; } = null!;
    public decimal MonthlyFee { get; set; }

    public ICollection<Instrument> Instruments { get; set; } = new List<Instrument>();
}
