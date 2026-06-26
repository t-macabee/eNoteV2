using eNote.Application.Common.Search;
using eNote.Domain.Enums;

namespace eNote.Application.Features.Rentals.InstrumentRentals;

public class InstrumentRentalSearchObject : BaseSearchObject
{
    public int? InstrumentId { get; set; }
    public InstrumentRentalStatus? RentalStatus { get; set; }
}
