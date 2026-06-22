using System.Text.Json;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.InstrumentRentals;
using eNote.Application.Features.InstrumentRentals.StateMachine;
using eNote.Contracts.Rentals;
using eNote.Domain.Entities;

namespace eNote.Infrastructure.Messaging;

public sealed class RentalNotificationDispatcher(
    IAppDbContext context,
    IClock clock) : IRentalNotificationDispatcher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task DispatchCreatedAsync(InstrumentRentalDto rental, int studentUserId, CancellationToken cancellationToken = default)
    {
        var message = new RentalStatusChanged(rental.Id, studentUserId, studentUserId, rental.RentalStatus.ToString(), rental.InstrumentModel, "Zahtjev za iznajmljivanje poslan", $"Vaš zahtjev za instrument {rental.InstrumentModel} je poslan prodavnici {rental.StoreName} i čeka odobrenje.", clock.UtcNow);

        return EnqueueOutboxAsync(message, cancellationToken);
    }

    public Task DispatchTransitionAsync(InstrumentRentalDto rental, RentalTrigger trigger, int actorUserId, CancellationToken cancellationToken = default)
    {
        (var title, var body) = BuildNotificationContent(rental, trigger);

        var message = new RentalStatusChanged(rental.Id, rental.StudentUserId, actorUserId, rental.RentalStatus.ToString(), rental.InstrumentModel, title, body, clock.UtcNow);

        return EnqueueOutboxAsync(message, cancellationToken);
    }

    private async Task EnqueueOutboxAsync(RentalStatusChanged message, CancellationToken cancellationToken)
    {
        var entry = new RentalNotificationOutbox
        {
            PayloadJson = JsonSerializer.Serialize(message, JsonOptions)
        };

        context.Set<RentalNotificationOutbox>().Add(entry);
        await context.SaveChangesAsync(cancellationToken);
    }

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