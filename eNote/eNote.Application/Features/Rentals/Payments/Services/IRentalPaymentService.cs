namespace eNote.Application.Features.Rentals.Payments.Services;

public interface IRentalPaymentService
{
    Task<CreatePaymentIntentResponse> CreatePaymentIntentAsync(int rentalId, CancellationToken cancellationToken = default);
    Task<RentalPaymentDto> GetPaymentStatusAsync(int rentalId, CancellationToken cancellationToken = default);
    Task<RentalPaymentDto> GetPaymentStatusForStoreAsync(int rentalId, CancellationToken cancellationToken = default);
    Task<RentalPaymentDto> RefundAsync(int rentalId, long? amountCents, CancellationToken cancellationToken = default);
}
