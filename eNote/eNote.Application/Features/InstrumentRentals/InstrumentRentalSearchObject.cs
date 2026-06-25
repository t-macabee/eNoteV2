using eNote.Application.Common.Search;
using eNote.Domain.Enums;

namespace eNote.Application.Features.InstrumentRentals;

public class InstrumentRentalSearchObject : BaseSearchObject
{
    public int? InstrumentId { get; set; }
    public InstrumentRentalStatus? RentalStatus { get; set; }
}
