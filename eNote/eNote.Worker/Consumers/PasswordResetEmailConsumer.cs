using eNote.Application.Common.Interfaces;
using eNote.Contracts.Identity;
using eNote.Infrastructure.Identity;
using MassTransit;
using Microsoft.AspNetCore.Identity;

namespace eNote.Worker.Consumers;

public sealed class PasswordResetEmailConsumer(
    UserManager<AppUser> userManager,
    IEmailService email,
    ILogger<PasswordResetEmailConsumer> logger) : IConsumer<PasswordResetRequested>
{
    public async Task Consume(ConsumeContext<PasswordResetRequested> context)
    {
        var message = context.Message;

        var user = await userManager.FindByIdAsync(message.UserId.ToString());
        if (user is null || !user.IsActive || string.IsNullOrWhiteSpace(user.Email))
        {
            logger.LogInformation(
                "Skipped password reset e-mail: recipient {UserId} is missing, inactive or has no e-mail.",
                message.UserId);
            return;
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        await email.SendPasswordResetAsync(user.Email, token, context.CancellationToken);
        logger.LogInformation("Sent password reset e-mail to recipient {UserId}.", message.UserId);
    }
}
