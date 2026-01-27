using eNote.Application.Common.Paging;
using eNote.Application.Common.Services.Interfaces;
using eNote.Application.Features.InstrumentRentals.DTOs;
using eNote.Application.Features.InstrumentRentals.Search;

namespace eNote.Application.Features.InstrumentRentals.Services.Interfaces
{
    public interface IRentalQueryService : IService<InstrumentRentalDto, InstrumentRentalSearchObject>
    {
        Task<InstrumentRentalDto> GetByIdForStudentAsync(int rentalId, int userId);
        Task<InstrumentRentalDto> GetByIdForShopAsync(int rentalId, int userId);

        Task<PagedResult<InstrumentRentalDto>> GetPagedForStudentAsync(int userId, InstrumentRentalSearchObject searchObject);
        Task<PagedResult<InstrumentRentalDto>> GetPagedForShopAsync(int userId, InstrumentRentalSearchObject searchObject);
    }
}
