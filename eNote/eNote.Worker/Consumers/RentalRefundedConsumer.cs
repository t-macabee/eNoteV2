using eNote.Application.Common.Persistence;
using eNote.Application.Constants;
using eNote.Contracts.Rentals;
using eNote.Domain.Entities.Communication;
using MassTransit;

namespace eNote.Worker.Consumers;

public sealed class RentalRefundedConsumer(IAppDbContext dbContext, ILogger<RentalRefundedConsumer> logger)
    : NotificationPersistenceConsumer<RentalRefunded>(dbContext, logger)
{
    protected override NotificationWrite Map(RentalRefunded message) => new(
        new Notification(message.StudentUserId, message.Title, message.Body, message.OccurredAtUtc, message.RentalId),
        "rental",
        "rental",
        message.RentalId,
        DbConstraintNames.NotificationUserRentalCreatedAtUniqueIndex);
}
