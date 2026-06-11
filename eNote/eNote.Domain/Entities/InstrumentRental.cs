using eNote.Domain.Entities.Base;
using eNote.Domain.Enums;

namespace eNote.Domain.Entities
{
    public class InstrumentRental : AuditableEntity
    {
        public decimal Fee { get; set; }
        public string? Note { get; set; }

        public DateTime RequestedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? PickedUpAt { get; set; }
        public DateTime? ReturnedAt { get; set; }

        public InstrumentRentalStatus RentalStatus { get; set; }

        public int StudentProfileId { get; set; }
        public Student StudentProfile { get; set; } = null!;

        public int InstrumentId { get; set; }
        public Instrument Instrument { get; set; } = null!;
    }
}
