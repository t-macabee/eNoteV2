using eNote.Domain.Entities.Base;

namespace eNote.Domain.Entities.Instruments
{
    public class InstrumentType : BaseEntity
    {
        public string Type { get; set; } = null!;

        public ICollection<Instrument> Instruments { get; set; } = new List<Instrument>();
    }
}
