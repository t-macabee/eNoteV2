using eNote.Application.DTOs;
using eNote.Application.Interfaces.Base;
using eNote.Application.SearchObjects;

namespace eNote.Application.Interfaces.Instruments.InstrumentRentals
{
    public interface IRentalQueryService : IService<InstrumentRentalDto, InstrumentRentalSearchObject>
    {
        Task<InstrumentRentalDto> GetByIdForStudentAsync(int rentalId, int userId);
        Task<InstrumentRentalDto> GetByIdForShopAsync(int rentalId, int userId);

        Task<PagedResult<InstrumentRentalDto>> GetPagedForStudentAsync(int userId, InstrumentRentalSearchObject searchObject);
        Task<PagedResult<InstrumentRentalDto>> GetPagedForShopAsync(int userId, InstrumentRentalSearchObject searchObject);
    }
}
