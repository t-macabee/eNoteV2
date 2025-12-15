namespace eNote.Domain.Entities
{
    public class InstrumentType
    {
        public int Id { get; set; }
        public string Type { get; set; } = null!;

        public ICollection<Instrument> Instruments { get; set; } = new List<Instrument>();
    }
}
