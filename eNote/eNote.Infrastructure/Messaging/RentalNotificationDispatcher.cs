using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Rentals.InstrumentRentals;
using eNote.Application.Features.Rentals.InstrumentRentals.Services;
using eNote.Contracts.Communication;
using eNote.Contracts.Rentals;
using eNote.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace eNote.Infrastructure.Messaging;

public sealed class RentalNotificationDispatcher(
    IAppDbContext context,
    IClock clock) : IRentalNotificationDispatcher
{
    public async Task DispatchCreatedAsync(InstrumentRentalDto rental, int studentUserId, CancellationToken cancellationToken = default)
    {
        var (title, body) = BuildCreatedContent(rental);
        var message = new RentalStatusChanged(rental.Id, studentUserId, studentUserId, rental.RentalStatus.ToString(), rental.InstrumentModel, title, body, clock.UtcNow);
        EnqueueOutbox(message);

        // Desktop-relevant notification: inform responsible StoreEmployees about the new request.
        // Follows the same outbox + Worker consumer pattern used for student notifications.
        var now = clock.UtcNow;
        var employeeUserIds = await LoadActiveEmployeeUserIdsAsync(rental.MusicStoreId, cancellationToken);

        foreach (var employeeUserId in employeeUserIds)
        {
            if (employeeUserId == studentUserId) continue;
            var (empTitle, empBody) = BuildStoreCreatedContent(rental);
            var empMessage = new RentalStatusChanged(rental.Id, employeeUserId, studentUserId, rental.RentalStatus.ToString(), rental.InstrumentModel, empTitle, empBody, now);
            EnqueueOutbox(empMessage);
        }
    }

    public async Task DispatchTransitionAsync(InstrumentRentalDto rental, RentalTrigger trigger, int actorUserId, CancellationToken cancellationToken = default)
    {
        // A cancel notifies the party that did not cancel: the student's cancel
        // informs the store; a store employee's cancel informs the student.
        if (trigger == RentalTrigger.Cancel && actorUserId == rental.StudentUserId)
        {
            var now = clock.UtcNow;
            var (empTitle, empBody) = BuildStoreCancelledContent(rental);
            foreach (var employeeUserId in await LoadActiveEmployeeUserIdsAsync(rental.MusicStoreId, cancellationToken))
            {
                if (employeeUserId == rental.StudentUserId) continue;
                var empMessage = new RentalStatusChanged(rental.Id, employeeUserId, actorUserId, rental.RentalStatus.ToString(), rental.InstrumentModel, empTitle, empBody, now);
                EnqueueOutbox(empMessage);
            }

            return;
        }

        var (title, body) = BuildNotificationContent(rental, trigger);
        var message = new RentalStatusChanged(rental.Id, rental.StudentUserId, actorUserId, rental.RentalStatus.ToString(), rental.InstrumentModel, title, body, clock.UtcNow);
        EnqueueOutbox(message);
    }

    private async Task<List<int>> LoadActiveEmployeeUserIdsAsync(int musicStoreId, CancellationToken cancellationToken) =>
        await context.Set<MusicStoreEmployee>()
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(x => x.MusicStoreId == musicStoreId && x.IsActive)
            .Join(context.Set<AppUser>().Where(user => user.IsActive),
                employee => employee.AppUserId,
                user => user.Id,
                (employee, _) => employee.AppUserId)
            .ToListAsync(cancellationToken);

    public Task DispatchPaymentRefundedAsync(InstrumentRentalDto rental, long refundedCents, string currency, int actorUserId)
    {
        var amount = refundedCents / 100m;
        var body = $"Za iznajmljivanje instrumenta {rental.InstrumentModel} vraćeno je {amount:F2} {FormatCurrency(currency)}.";
        var message = new RentalRefunded(
            rental.Id,
            rental.StudentUserId,
            actorUserId,
            refundedCents,
            currency,
            rental.InstrumentModel,
            "Uplata vraćena",
            body,
            clock.UtcNow);

        EnqueueRefundOutbox(message);
        return Task.CompletedTask;
    }

    public async Task DispatchPaymentSucceededAsync(int rentalId, long amountCents, string currency, CancellationToken cancellationToken = default)
    {
        // The Stripe webhook has no mapped DTO; load only what the message needs.
        // Filters are ignored because a paid rental's instrument can be deactivated.
        var rental = await context.Set<InstrumentRental>()
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(r => r.Id == rentalId)
            .Select(r => new { StudentUserId = r.StudentProfile.AppUserId, r.Instrument.Model, r.RentalStatus })
            .SingleAsync(cancellationToken);

        var amount = amountCents / 100m;
        var body = $"Plaćanje za instrument {rental.Model} je uspješno. Iznos: {amount:F2} {FormatCurrency(currency)}.";
        var message = new RentalStatusChanged(rentalId, rental.StudentUserId, rental.StudentUserId, rental.RentalStatus.ToString(), rental.Model, "Plaćanje uspješno", body, clock.UtcNow);
        EnqueueOutbox(message);
    }

    // Currency display contract, mirrored in
    // UI/enote_core/lib/formatting/formatters.dart (_currencyLabel):
    // an empty code or the BAM code is displayed as KM; every other code
    // is upper-cased.
    private const string LocalCurrencyCode = "bam";
    private const string LocalCurrencyLabel = "KM";

    private static string FormatCurrency(string currency) =>
        string.IsNullOrEmpty(currency) || currency.Equals(LocalCurrencyCode, StringComparison.OrdinalIgnoreCase) ? LocalCurrencyLabel
        : currency.ToUpperInvariant();

    private static (string Title, string Body) BuildCreatedContent(InstrumentRentalDto rental) =>
        ("Zahtjev za iznajmljivanje poslan", $"Vaš zahtjev za instrument {rental.InstrumentModel} je poslan prodavnici {rental.StoreName} i čeka odobrenje.");

    private static (string Title, string Body) BuildStoreCreatedContent(InstrumentRentalDto rental) =>
        ("Novi zahtjev za iznajmljivanje", $"Zaprimljen je novi zahtjev za instrument {rental.InstrumentModel} u prodavnici {rental.StoreName}.");

    private static (string Title, string Body) BuildStoreCancelledContent(InstrumentRentalDto rental) =>
        ("Zahtjev otkazan", string.IsNullOrWhiteSpace(rental.Note) ? $"Student je otkazao zahtjev za instrument {rental.InstrumentModel} u prodavnici {rental.StoreName}." : $"Student je otkazao zahtjev za instrument {rental.InstrumentModel} u prodavnici {rental.StoreName}. Razlog: {rental.Note}");

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
                ("Zahtjev otkazan", string.IsNullOrWhiteSpace(rental.Note) ? $"Vaš zahtjev za instrument {rental.InstrumentModel} je otkazan." : $"Vaš zahtjev za instrument {rental.InstrumentModel} je otkazan. Razlog: {rental.Note}"),
            RentalTrigger.ReturnEarly =>
                ("Instrument vraćen prije roka", $"Instrument {rental.InstrumentModel} je vraćen prije planiranog roka."),
            _ => ("Status iznajmljivanja promijenjen", $"Status iznajmljivanja za instrument {rental.InstrumentModel} je ažuriran.")
        };
}