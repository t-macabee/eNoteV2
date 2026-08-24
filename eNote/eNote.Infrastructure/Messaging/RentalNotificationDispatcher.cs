using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Rentals.InstrumentRentals;
using eNote.Application.Features.Rentals.InstrumentRentals.Services;
using eNote.Contracts.Communication;
using eNote.Contracts.Rentals;

namespace eNote.Infrastructure.Messaging;

public sealed class RentalNotificationDispatcher(
    IAppDbContext context,
    IClock clock) : IRentalNotificationDispatcher
{
    public Task DispatchCreatedAsync(InstrumentRentalDto rental, int studentUserId)
    {
        var (title, body) = BuildCreatedContent(rental);
        var message = new RentalStatusChanged(rental.Id, studentUserId, studentUserId, rental.RentalStatus.ToString(), rental.InstrumentModel, title, body, clock.UtcNow);
        EnqueueOutbox(message);
        return Task.CompletedTask;
    }

    public Task DispatchTransitionAsync(InstrumentRentalDto rental, RentalTrigger trigger, int actorUserId)
    {
        var (title, body) = BuildNotificationContent(rental, trigger);
        var message = new RentalStatusChanged(rental.Id, rental.StudentUserId, actorUserId, rental.RentalStatus.ToString(), rental.InstrumentModel, title, body, clock.UtcNow);
        EnqueueOutbox(message);
        return Task.CompletedTask;
    }

    public Task DispatchPaymentRefundedAsync(InstrumentRentalDto rental, long refundedCents, int actorUserId)
    {
        var amount = refundedCents / 100m;
        var message = new RentalRefunded(
            rental.Id,
            rental.StudentUserId,
            actorUserId,
            refundedCents,
            rental.InstrumentModel,
            "Uplata vraćena",
            $"Za iznajmljivanje instrumenta {rental.InstrumentModel} vraćeno je {amount:F2} EUR.",
            clock.UtcNow);

        EnqueueRefundOutbox(message);
        return Task.CompletedTask;
    }

    private static (string Title, string Body) BuildCreatedContent(InstrumentRentalDto rental) =>
        ("Zahtjev za iznajmljivanje poslan", $"Vaš zahtjev za instrument {rental.InstrumentModel} je poslan prodavnici {rental.StoreName} i čeka odobrenje.");

    private void EnqueueOutbox(RentalStatusChanged message) =>
        NotificationOutboxWriter.Enqueue(context, NotificationMessageTypes.RentalStatusChanged, message);

    private void EnqueueRefundOutbox(RentalRefunded message) =>
        NotificationOutboxWriter.Enqueue(context, NotificationMessageTypes.PaymentRefunded, message);

    private static (string Title, string Body) BuildNotificationContent(InstrumentRentalDto rental, RentalTrigger trigger) =>
        trigger switch
        {
            RentalTrigger.Approve =>
                ("Zahtjev odobren", $"Vaš zahtjev za instrument {rental.InstrumentModel} je odobren. Mjesečna naknada: {rental.Fee:F2} KM."),
            RentalTrigger.Reject =>
                ("Zahtjev odbijen", string.IsNullOrWhiteSpace(rental.Note) ? $"Vaš zahtjev za instrument {rental.InstrumentModel} je odbijen." : $"Vaš zahtjev za instrument {rental.InstrumentModel} je odbijen. Razlog: {rental.Note}"),
            RentalTrigger.Pickup =>
                ("Instrument preuzet", $"Preuzeli ste instrument {rental.InstrumentModel}."),
            RentalTrigger.Complete =>
                ("Iznajmljivanje završeno", $"Iznajmljivanje instrumenta {rental.InstrumentModel} je uspješno završeno."),
            RentalTrigger.Cancel =>
                ("Zahtjev otkazan", $"Vaš zahtjev za instrument {rental.InstrumentModel} je otkazan."),
            RentalTrigger.ReturnEarly =>
                ("Instrument vraćen prije roka", $"Instrument {rental.InstrumentModel} je vraćen prije planiranog roka."),
            _ => ("Status iznajmljivanja promijenjen", $"Status iznajmljivanja za instrument {rental.InstrumentModel} je ažuriran.")
        };
}