using eNote.Application.DTOs;
using eNote.Application.Requests.InstrumentRental;
using eNote.Application.SearchObjects;

namespace eNote.Application.Interfaces.Instruments.InstrumentRentals
{
    public interface IRentalService
    {        
        Task<InstrumentRentalDto> GetByIdForStudentAsync(int rentalId, int userId);
        Task<InstrumentRentalDto> GetByIdForShopAsync(int rentalId, int userId);

        Task<PagedResult<InstrumentRentalDto>> GetPagedForStudentAsync(int userId, InstrumentRentalSearchObject searchObject);
        Task<PagedResult<InstrumentRentalDto>> GetPagedForShopAsync(int userId, InstrumentRentalSearchObject searchObject);

        Task<InstrumentRentalDto> CreateRequestAsync(int studentId, RentalCreateRequest request);
        Task<InstrumentRentalDto> ApproveAsync(int rentalId, int userId, RentalStatusResponse response);
        Task<InstrumentRentalDto> RejectAsync(int rentalId, int userId, RentalStatusResponse response);
        Task<InstrumentRentalDto> PickupAsync(int rentalId, int userId, RentalStatusResponse response);
        Task<InstrumentRentalDto> CompleteAsync(int rentalId, int userId, RentalStatusResponse response);
    }
}
