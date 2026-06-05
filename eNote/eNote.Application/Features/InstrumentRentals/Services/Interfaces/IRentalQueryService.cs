using eNote.Application.Common.Paging;
using eNote.Application.Features.InstrumentRentals.Search;

namespace eNote.Application.Features.InstrumentRentals.Services.Interfaces
{
    public interface IRentalQueryService
    {
        Task<InstrumentRentalDto> GetByIdForStudentAsync(int rentalId, int userId);
        Task<InstrumentRentalDto> GetByIdForStoreAsync(int rentalId, int userId);

        Task<PagedResult<InstrumentRentalDto>> GetPagedForStudentAsync(int userId, InstrumentRentalSearchObject searchObject);
        Task<PagedResult<InstrumentRentalDto>> GetPagedForStoreAsync(int userId, InstrumentRentalSearchObject searchObject);
    }
}
