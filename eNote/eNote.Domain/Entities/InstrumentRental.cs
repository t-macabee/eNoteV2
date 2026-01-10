using eNote.Domain.Entities.Base;
using eNote.Domain.Entities.Users;
using eNote.Domain.Enums;

namespace eNote.Domain.Entities
{
    public class InstrumentRental : BaseEntity
    {
        public decimal Fee { get; set; }
        public string? Note { get; set; }
        public DateTime RentedAt { get; set; }
        public DateTime? ReturnedAt { get; set; }
        public InstrumentRentalStatus RentalStatus { get; set; }

        public int StudentId { get; set; }
        public Student Student { get; set; } = null!;
        public int InstrumentId { get; set; }
        public Instrument Instrument { get; set; } = null!;
    }
}
