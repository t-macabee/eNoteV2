using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Time;
using eNote.Application.Features.InstrumentRentals;
using eNote.Application.Features.InstrumentRentals.StateMachine;
using eNote.Contracts.Rentals;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace eNote.Infrastructure.Messaging;

public sealed class RentalNotificationDispatcher(IPublishEndpoint publishEndpoint, IClock clock, ILogger<RentalNotificationDispatcher> logger) : IRentalNotificationDispatcher
{
    private const int MaxPublishAttempts = 3;

    public Task DispatchCreatedAsync(InstrumentRentalDto rental, int studentUserId, CancellationToken cancellationToken = default)
    {
        var message = new RentalStatusChanged(rental.Id, studentUserId, studentUserId, rental.RentalStatus.ToString(), rental.InstrumentModel, "Zahtjev za iznajmljivanje poslan", $"Vaš zahtjev za instrument {rental.InstrumentModel} je poslan prodavnici {rental.StoreName} i čeka odobrenje.", clock.UtcNow);

        return PublishWithRetryAsync(message, rental.Id, cancellationToken);
    }

    public Task DispatchTransitionAsync(InstrumentRentalDto rental, RentalTrigger trigger, int actorUserId, CancellationToken cancellationToken = default)
    {
        (var title, var body) = BuildNotificationContent(rental, trigger);

        var message = new RentalStatusChanged(rental.Id, rental.StudentUserId, actorUserId, rental.RentalStatus.ToString(), rental.InstrumentModel, title, body, clock.UtcNow);

        return PublishWithRetryAsync(message, rental.Id, cancellationToken);
    }

    private async Task PublishWithRetryAsync(RentalStatusChanged message, int rentalId, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxPublishAttempts; attempt++)
        {
            try
            {
                await publishEndpoint.Publish(message, cancellationToken);
                return;
            }
            catch (Exception ex) when (attempt < MaxPublishAttempts)
            {
                logger.LogWarning(ex, "RabbitMQ publish attempt {Attempt}/{MaxAttempts} failed for rental {RentalId}.", attempt, MaxPublishAttempts, rentalId);
                await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt), cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "RabbitMQ publish failed after {MaxAttempts} attempts for rental {RentalId}.", MaxPublishAttempts, rentalId);
                throw;
            }
        }
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
