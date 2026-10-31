using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Rentals.InstrumentRentals;
using eNote.Application.Features.Rentals.InstrumentRentals.Services;
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
        var message = new RentalStatusChanged(rental.Id, studentUserId, studentUserId, studentUserId, rental.RentalStatus.ToString(), rental.InstrumentModel, title, body, clock.UtcNow);
        EnqueueOutbox(message);

        var now = clock.UtcNow;
        var employeeUserIds = await LoadActiveEmployeeUserIdsAsync(rental.MusicStoreId, cancellationToken);

        foreach (var employeeUserId in employeeUserIds)
        {
            if (employeeUserId == studentUserId) continue;
            var (empTitle, empBody) = BuildStoreCreatedContent(rental);
            var empMessage = new RentalStatusChanged(rental.Id, studentUserId, employeeUserId, studentUserId, rental.RentalStatus.ToString(), rental.InstrumentModel, empTitle, empBody, now);
            EnqueueOutbox(empMessage);
        }
    }

    public async Task DispatchTransitionAsync(InstrumentRentalDto rental, RentalTrigger trigger, int? actorUserId, CancellationToken cancellationToken = default)
    {
        if (trigger == RentalTrigger.Renew)
        {
            await DispatchToStoreEmployeesAsync(
                rental,
                actorUserId,
                "Iznajmljivanje produženo",
                $"Iznajmljivanje instrumenta {rental.InstrumentModel} ({rental.StudentName}) je produženo do {rental.DueAt:dd.MM.yyyy}.",
                cancellationToken);
            return;
        }

        if (trigger == RentalTrigger.Cancel && actorUserId == rental.StudentUserId)
        {
            var (empTitle, empBody) = BuildStoreCancelledContent(rental);
            await DispatchToStoreEmployeesAsync(rental, actorUserId, empTitle, empBody, cancellationToken);
            return;
        }

        var (title, body) = BuildNotificationContent(rental, trigger);
        var message = new RentalStatusChanged(rental.Id, rental.StudentUserId, rental.StudentUserId, actorUserId, rental.RentalStatus.ToString(), rental.InstrumentModel, title, body, clock.UtcNow);
        EnqueueOutbox(message);
    }

    private async Task DispatchToStoreEmployeesAsync(InstrumentRentalDto rental, int? actorUserId, string title, string body, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        foreach (var employeeUserId in await LoadActiveEmployeeUserIdsAsync(rental.MusicStoreId, cancellationToken))
        {
            if (employeeUserId == rental.StudentUserId) continue;
            var empMessage = new RentalStatusChanged(rental.Id, rental.StudentUserId, employeeUserId, actorUserId, rental.RentalStatus.ToString(), rental.InstrumentModel, title, body, now);
            EnqueueOutbox(empMessage);
        }
    }

    private async Task<List<int>> LoadActiveEmployeeUserIdsAsync(int musicStoreId, CancellationToken cancellationToken) =>
        await context.Set<MusicStoreEmployee>()
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(x => x.MusicStoreId == musicStoreId)
            .Join(context.Set<AppUser>().Where(user => user.IsActive),
                employee => employee.AppUserId,
                user => user.Id,
                (employee, _) => employee.AppUserId)
            .ToListAsync(cancellationToken);

    public Task DispatchPaymentRefundedAsync(InstrumentRentalDto rental, long refundedCents, string currency, int actorUserId)
    {
        var body = $"Za iznajmljivanje instrumenta {rental.InstrumentModel} vraćeno je {CurrencyAmount.Format(refundedCents, currency)}.";
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
        var rental = await LoadPaymentTargetAsync(rentalId, cancellationToken);

        var body = $"Plaćanje za instrument {rental.Model} je uspješno. Iznos: {CurrencyAmount.Format(amountCents, currency)}.";
        var message = new RentalStatusChanged(rentalId, rental.StudentUserId, rental.StudentUserId, rental.StudentUserId, rental.RentalStatus.ToString(), rental.Model, "Plaćanje uspješno", body, clock.UtcNow);
        EnqueueOutbox(message);
    }

    public async Task DispatchDuplicatePaymentRefundedAsync(int rentalId, long amountCents, string currency, CancellationToken cancellationToken = default)
    {
        var rental = await LoadPaymentTargetAsync(rentalId, cancellationToken);

        var body = $"Dupla uplata je automatski vraćena. Iznos: {CurrencyAmount.Format(amountCents, currency)}.";
        var message = new RentalStatusChanged(rentalId, rental.StudentUserId, rental.StudentUserId, rental.StudentUserId, rental.RentalStatus.ToString(), rental.Model, "Dupla uplata vraćena", body, clock.UtcNow);
        EnqueueOutbox(message);
    }

    private async Task<PaymentTarget> LoadPaymentTargetAsync(int rentalId, CancellationToken cancellationToken) =>
        await context.Set<InstrumentRental>()
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(r => r.Id == rentalId)
            .Select(r => new PaymentTarget(r.StudentProfile.AppUserId, r.Instrument.Model, r.RentalStatus))
            .SingleAsync(cancellationToken);

    private sealed record PaymentTarget(int StudentUserId, string Model, InstrumentRentalStatus RentalStatus);

    private static (string Title, string Body) BuildCreatedContent(InstrumentRentalDto rental)
    {
        var body = $"Vaš zahtjev za instrument {rental.InstrumentModel} je poslan prodavnici {rental.StoreName} i čeka odobrenje.";

        if (rental.OtherPendingRequests > 0)
        {
            body += $" Broj zahtjeva na čekanju za isti instrument: {rental.OtherPendingRequests}.";
        }

        return ("Zahtjev za iznajmljivanje poslan", body);
    }

    private static (string Title, string Body) BuildStoreCreatedContent(InstrumentRentalDto rental) =>
        ("Novi zahtjev za iznajmljivanje", $"Zaprimljen je novi zahtjev za instrument {rental.InstrumentModel} u prodavnici {rental.StoreName}.");

    private static (string Title, string Body) BuildStoreCancelledContent(InstrumentRentalDto rental) =>
        ("Zahtjev otkazan", string.IsNullOrWhiteSpace(rental.Note) ? $"Student je otkazao zahtjev za instrument {rental.InstrumentModel} u prodavnici {rental.StoreName}." : $"Student je otkazao zahtjev za instrument {rental.InstrumentModel} u prodavnici {rental.StoreName}. Razlog: {rental.Note}");

    private void EnqueueOutbox(RentalStatusChanged message) =>
        NotificationOutboxWriter.Enqueue(context, message);

    private void EnqueueRefundOutbox(RentalRefunded message) =>
        NotificationOutboxWriter.Enqueue(context, message);

    private static (string Title, string Body) BuildNotificationContent(InstrumentRentalDto rental, RentalTrigger trigger) =>
        trigger switch
        {
            RentalTrigger.Approve =>
                ("Zahtjev odobren", $"Vaš zahtjev za instrument {rental.InstrumentModel} je odobren. Mjesečna naknada: {CurrencyAmount.Local(rental.Fee)}. Instrument preuzmite u roku od {InstrumentRental.PickupWindow.Days} dana. Za svaki dan kašnjenja nakon isteka roka plaća se penal od {CurrencyAmount.Local(rental.DailyPenaltyFee)}."),
            RentalTrigger.Reject or RentalTrigger.ExpirePickup =>
                ("Zahtjev odbijen", string.IsNullOrWhiteSpace(rental.Note) ? $"Vaš zahtjev za instrument {rental.InstrumentModel} je odbijen." : $"Vaš zahtjev za instrument {rental.InstrumentModel} je odbijen. Razlog: {rental.Note}"),
            RentalTrigger.Pickup =>
                ("Instrument preuzet", $"Preuzeli ste instrument {rental.InstrumentModel}. Rok povrata je {rental.DueAt:dd.MM.yyyy}. Produženje je moguće u zadnjih {InstrumentRental.RenewalWindow.Days} dana roka."),
            RentalTrigger.Complete =>
                ("Iznajmljivanje završeno", $"Iznajmljivanje instrumenta {rental.InstrumentModel} je uspješno završeno."),
            RentalTrigger.Cancel =>
                ("Zahtjev otkazan", string.IsNullOrWhiteSpace(rental.Note) ? $"Vaš zahtjev za instrument {rental.InstrumentModel} je otkazan." : $"Vaš zahtjev za instrument {rental.InstrumentModel} je otkazan. Razlog: {rental.Note}"),
            RentalTrigger.ReturnEarly =>
                ("Instrument vraćen prije roka", $"Instrument {rental.InstrumentModel} je vraćen prije planiranog roka."),
            _ => ("Status iznajmljivanja promijenjen", $"Status iznajmljivanja za instrument {rental.InstrumentModel} je ažuriran.")
        };
}