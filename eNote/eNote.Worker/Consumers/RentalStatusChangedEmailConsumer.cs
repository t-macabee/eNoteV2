using eNote.Application.Common.Interfaces;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Contracts.Rentals;
using eNote.Domain.Enums;
using MassTransit;

namespace eNote.Worker.Consumers;

public sealed class RentalStatusChangedEmailConsumer(
    IUserIdentityService users,
    IEmailService email,
    ILogger<RentalStatusChangedEmailConsumer> logger) : IConsumer<RentalStatusChanged>
{
    // The decisions the recipient must not miss.
    private static readonly HashSet<string> EmailStatuses =
    [
        nameof(InstrumentRentalStatus.Approved),
        nameof(InstrumentRentalStatus.Rejected),
        nameof(InstrumentRentalStatus.Canceled)
    ];

    public async Task Consume(ConsumeContext<RentalStatusChanged> context)
    {
        var message = context.Message;

        if (!EmailStatuses.Contains(message.Status))
        {
            return;
        }

        var user = await users.GetUserAsync(message.StudentUserId, context.CancellationToken);
        if (user is null || !user.IsActive || string.IsNullOrWhiteSpace(user.Email))
        {
            logger.LogInformation(
                "Skipped rental {RentalId} e-mail: recipient {UserId} is missing, inactive or has no e-mail.",
                message.RentalId,
                message.StudentUserId);
            return;
        }

        await email.SendNotificationAsync(user.Email, message.Title, message.Body, context.CancellationToken);
        logger.LogInformation(
            "Sent rental {RentalId} e-mail to recipient {UserId}.",
            message.RentalId,
            message.StudentUserId);
    }
}
