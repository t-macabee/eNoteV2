namespace eNote.Application.Features.Rentals.InstrumentRentals.Services;

public interface IRentalCommandService
{
    Task<InstrumentRentalDto> CreateRequestAsync(RentalCreateRequest request, CancellationToken cancellationToken = default);
    Task<InstrumentRentalDto> ApproveAsync(int rentalId, RentalStatusResponse response, CancellationToken cancellationToken = default);
    Task<InstrumentRentalDto> RejectAsync(int rentalId, RentalStatusResponse response, CancellationToken cancellationToken = default);
    Task<InstrumentRentalDto> PickupAsync(int rentalId, RentalStatusResponse response, CancellationToken cancellationToken = default);
    Task<InstrumentRentalDto> CompleteAsync(int rentalId, RentalStatusResponse response, CancellationToken cancellationToken = default);
    Task<InstrumentRentalDto> CancelAsync(int rentalId, RentalStatusResponse response, CancellationToken cancellationToken = default);
    Task<InstrumentRentalDto> ReturnEarlyAsync(int rentalId, RentalStatusResponse response, CancellationToken cancellationToken = default);
}
