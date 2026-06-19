using eNote.Domain.Entities.Base;
using eNote.Domain.Enums;

namespace eNote.Domain.Entities
{
    public class InstrumentRental : AuditableEntity
    {
        public int StudentProfileId { get; private set; }
        public Student StudentProfile { get; private set; } = null!;
        public int InstrumentId { get; private set; }
        public Instrument Instrument { get; private set; } = null!;

        public InstrumentRentalStatus RentalStatus { get; private set; }
        public string? RequestNote { get; private set; }
        public string? Note { get; private set; }

        public DateTime RequestedAt { get; private set; }
        public DateTime? ApprovedAt { get; private set; }
        public DateTime? RejectedAt { get; private set; }
        public DateTime? PickedUpAt { get; private set; }
        public DateTime? ReturnedAt { get; private set; }

        public int? ApprovedById { get; private set; }
        public int? RejectedById { get; private set; }

        public decimal Fee { get; private set; }

        protected InstrumentRental()
        {
        }

        public InstrumentRental(int instrumentId, int studentProfileId, DateTime requestedAt, string? note)
        {
            InstrumentId = instrumentId;
            StudentProfileId = studentProfileId;
            RequestedAt = requestedAt;
            RequestNote = note;
            RentalStatus = InstrumentRentalStatus.Pending;
        }

        public void Approve(decimal fee, string? note, DateTime approvedAt, int approvedById)
        {
            Fee = fee;
            Note = note;
            ApprovedAt = approvedAt;
            ApprovedById = approvedById;
            RentalStatus = InstrumentRentalStatus.Approved;
        }

        public void Reject(DateTime rejectedAt, string? note, int rejectedById)
        {
            Note = note;
            RejectedAt = rejectedAt;
            RejectedById = rejectedById;
            RentalStatus = InstrumentRentalStatus.Rejected;
        }

        public void Cancel(DateTime returnedAt, string? note)
        {
            Note = note;
            ReturnedAt = returnedAt;
            RentalStatus = InstrumentRentalStatus.Canceled;
        }

        public void Pickup(DateTime pickedUpAt, string? note = null)
        {
            PickedUpAt = pickedUpAt;
            RentalStatus = InstrumentRentalStatus.Active;
            if (note != null)
            {
                Note = note;
            }
        }

        public void Complete(DateTime returnedAt, string? note)
        {
            Note = note;
            ReturnedAt = returnedAt;
            RentalStatus = InstrumentRentalStatus.Completed;
        }

        public void ReturnEarly(DateTime returnedAt, string? note)
        {
            Note = note;
            ReturnedAt = returnedAt;
            RentalStatus = InstrumentRentalStatus.ReturnedEarly;
        }
    }
}
