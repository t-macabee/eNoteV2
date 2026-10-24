using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Identity.Students.Services;
using eNote.Contracts.Communication;
using eNote.Contracts.Identity;

namespace eNote.Infrastructure.Messaging;

public sealed class MembershipWarningDispatcher(
    IAppDbContext context,
    IClock clock) : IMembershipWarningDispatcher
{
    public Task DispatchWarningAsync(int studentUserId, DateTime? membershipPaidUntil, string? message)
    {
        var body = !string.IsNullOrWhiteSpace(message)
            ? message.Trim()
            : membershipPaidUntil.HasValue
                ? $"Vaša članarina ističe {membershipPaidUntil.Value:dd.MM.yyyy}. Obnovite je da biste nastavili koristiti usluge."
                : "Vaša članarina nije evidentirana. Kontaktirajte administratora.";

        var warning = new MembershipWarning(studentUserId, "Upozorenje o članarini", body, clock.UtcNow);
        NotificationOutboxWriter.Enqueue(context, NotificationMessageTypes.MembershipWarning, warning);

        return Task.CompletedTask;
    }
}
