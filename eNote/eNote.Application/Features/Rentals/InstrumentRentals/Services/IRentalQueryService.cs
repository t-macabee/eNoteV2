using eNote.Application.Common.Paging;
using eNote.Application.Features.Rentals.InstrumentRentals;

namespace eNote.Application.Features.Rentals.InstrumentRentals.Services;

public interface IRentalQueryService
{
    Task<InstrumentRentalDto> GetByIdForStudentAsync(int rentalId);
    Task<InstrumentRentalDto> GetByIdForStoreAsync(int rentalId);
    Task<PagedResult<InstrumentRentalDto>> GetPagedForStudentAsync(InstrumentRentalSearchObject searchObject);
    Task<PagedResult<InstrumentRentalDto>> GetPagedForStoreAsync(InstrumentRentalSearchObject searchObject);
}
