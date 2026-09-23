namespace eNote.Application.Features.Rentals.InstrumentRentals.Services;

public interface IRentalNotificationDispatcher
{
    Task DispatchCreatedAsync(InstrumentRentalDto rental, int studentUserId, CancellationToken cancellationToken = default);
    Task DispatchTransitionAsync(InstrumentRentalDto rental, RentalTrigger trigger, int actorUserId, CancellationToken cancellationToken = default);
    Task DispatchPaymentRefundedAsync(InstrumentRentalDto rental, long refundedCents, string currency, int actorUserId);
}
