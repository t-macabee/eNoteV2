using eNote.Application.DTOs;
using eNote.Application.Requests.InstrumentRental;

namespace eNote.Application.Interfaces.InstrumentRentals
{
    public interface IRentalCommandService
    {
        Task<InstrumentRentalDto> CreateRequestAsync(int userId, RentalCreateRequest request);
        Task<InstrumentRentalDto> ApproveAsync(int rentalId, int userId, RentalStatusResponse response);
        Task<InstrumentRentalDto> RejectAsync(int rentalId, int userId, RentalStatusResponse response);
        Task<InstrumentRentalDto> PickupAsync(int rentalId, int userId, RentalStatusResponse response);
        Task<InstrumentRentalDto> CompleteAsync(int rentalId, int userId, RentalStatusResponse response);
    }
}
