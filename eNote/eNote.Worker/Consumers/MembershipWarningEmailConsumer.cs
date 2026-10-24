using eNote.Application.Common.Interfaces;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Contracts.Identity;
using MassTransit;

namespace eNote.Worker.Consumers;

public sealed class MembershipWarningEmailConsumer(
    IUserIdentityService users,
    IEmailService email,
    ILogger<MembershipWarningEmailConsumer> logger) : IConsumer<MembershipWarning>
{
    public async Task Consume(ConsumeContext<MembershipWarning> context)
    {
        var message = context.Message;

        var user = await users.GetUserAsync(message.StudentUserId, context.CancellationToken);
        if (user is null || !user.IsActive || string.IsNullOrWhiteSpace(user.Email))
        {
            logger.LogInformation(
                "Skipped membership warning e-mail: recipient {UserId} is missing, inactive or has no e-mail.",
                message.StudentUserId);
            return;
        }

        await email.SendNotificationAsync(user.Email, message.Title, message.Body, context.CancellationToken);
        logger.LogInformation(
            "Sent membership warning e-mail to recipient {UserId}.",
            message.StudentUserId);
    }
}
