namespace eNote.Application.Features.Rentals.InstrumentRentals.Services;

public interface IRentalNotificationDispatcher
{
    Task DispatchCreatedAsync(InstrumentRentalDto rental, int studentUserId);
    Task DispatchTransitionAsync(InstrumentRentalDto rental, RentalTrigger trigger, int actorUserId);
    Task DispatchPaymentRefundedAsync(InstrumentRentalDto rental, long refundedCents, int actorUserId);
}
