using eNote.Application.Common.Paging;

namespace eNote.Application.Features.InstrumentRentals.Services
{
    public interface IRentalQueryService
    {
        Task<InstrumentRentalDto> GetByIdForStudentAsync(int rentalId);
        Task<InstrumentRentalDto> GetByIdForStoreAsync(int rentalId);
        Task<PagedResult<InstrumentRentalDto>> GetPagedForStudentAsync(InstrumentRentalSearchObject searchObject);
        Task<PagedResult<InstrumentRentalDto>> GetPagedForStoreAsync(InstrumentRentalSearchObject searchObject);
    }
}
