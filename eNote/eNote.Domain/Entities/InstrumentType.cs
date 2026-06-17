using eNote.Domain.Entities.Base;

namespace eNote.Domain.Entities
{
    public class InstrumentType : BaseEntity
    {
        public string Type { get; set; } = null!;
        public decimal MonthlyFee
        {
            get; set;
        }

        public ICollection<Instrument> Instruments { get; set; } = new List<Instrument>();
    }
}
