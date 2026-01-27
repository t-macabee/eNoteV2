using eNote.Application.Common.Paging;
using eNote.Application.Features.InstrumentRentals.DTOs;
using eNote.Application.Features.InstrumentRentals.Requests;
using eNote.Application.Features.InstrumentRentals.Search;

namespace eNote.Application.Features.InstrumentRentals.Services.Interfaces
{
    public interface IRentalService
    {        
        Task<InstrumentRentalDto> GetByIdForStudentAsync(int rentalId, int userId);
        Task<InstrumentRentalDto> GetByIdForShopAsync(int rentalId, int userId);

        Task<PagedResult<InstrumentRentalDto>> GetPagedForStudentAsync(int userId, InstrumentRentalSearchObject searchObject);
        Task<PagedResult<InstrumentRentalDto>> GetPagedForShopAsync(int userId, InstrumentRentalSearchObject searchObject);

        Task<InstrumentRentalDto> CreateRequestAsync(int userId, RentalCreateRequest request);
        Task<InstrumentRentalDto> ApproveAsync(int rentalId, int userId, RentalStatusResponse response);
        Task<InstrumentRentalDto> RejectAsync(int rentalId, int userId, RentalStatusResponse response);
        Task<InstrumentRentalDto> PickupAsync(int rentalId, int userId, RentalStatusResponse response);
        Task<InstrumentRentalDto> CompleteAsync(int rentalId, int userId, RentalStatusResponse response);
        Task<InstrumentRentalDto> CancelAsync(int rentalId, int userId, RentalStatusResponse response);
        Task<InstrumentRentalDto> ReturnEarlyAsync(int rentalId, int userId, RentalStatusResponse response);
    }
}
