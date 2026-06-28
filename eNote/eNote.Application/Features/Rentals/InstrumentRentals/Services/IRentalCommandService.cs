namespace eNote.Application.Features.Rentals.InstrumentRentals.Services;

public interface IRentalCommandService
{
    Task<InstrumentRentalDto> CreateRequestAsync(RentalCreateRequest request);
    Task<InstrumentRentalDto> ApproveAsync(int rentalId, RentalStatusResponse response);
    Task<InstrumentRentalDto> RejectAsync(int rentalId, RentalStatusResponse response);
    Task<InstrumentRentalDto> PickupAsync(int rentalId, RentalStatusResponse response);
    Task<InstrumentRentalDto> CompleteAsync(int rentalId, RentalStatusResponse response);
    Task<InstrumentRentalDto> CancelAsync(int rentalId, RentalStatusResponse response);
    Task<InstrumentRentalDto> ReturnEarlyAsync(int rentalId, RentalStatusResponse response);
}
