using eNote.Application.Features.InstrumentRentals.DTOs;
using eNote.Application.Features.InstrumentRentals.Requests;

namespace eNote.Application.Features.InstrumentRentals.Services.Interfaces
{
    public interface IRentalCommandService
    {
        Task<InstrumentRentalDto> CreateRequestAsync(int userId, RentalCreateRequest request);
        Task<InstrumentRentalDto> ApproveAsync(int rentalId, int userId, RentalStatusResponse response);
        Task<InstrumentRentalDto> RejectAsync(int rentalId, int userId, RentalStatusResponse response);
        Task<InstrumentRentalDto> PickupAsync(int rentalId, int userId, RentalStatusResponse response);
        Task<InstrumentRentalDto> CompleteAsync(int rentalId, int userId, RentalStatusResponse response);
        Task<InstrumentRentalDto> CancelAsync(int rentalId, int userId, RentalStatusResponse response);
        Task<InstrumentRentalDto> ReturnEarlyAsync(int rentalId, int userId, RentalStatusResponse response);
    }
}
