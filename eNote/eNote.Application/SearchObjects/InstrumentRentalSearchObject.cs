using eNote.Domain.Enums;

namespace eNote.Application.SearchObjects
{
    public class InstrumentRentalSearchObject : BaseSearchObject
    {
        public int? InstrumentId { get; set; }
        public InstrumentRentalStatus? RentalStatus { get; set; }
    }
}
