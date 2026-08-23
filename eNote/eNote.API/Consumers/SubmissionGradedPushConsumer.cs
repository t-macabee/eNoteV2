using eNote.API.Hubs;
using eNote.Application.Features.Communication.Notifications;
using eNote.Contracts.Assignments;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace eNote.API.Consumers;

public sealed class SubmissionGradedPushConsumer(IHubContext<NotificationHub> hubContext, ILogger<SubmissionGradedPushConsumer> logger) : IConsumer<SubmissionGraded>
{
    public async Task Consume(ConsumeContext<SubmissionGraded> context)
    {
        var message = context.Message;

        var payload = new NotificationPushDto()
        {
            SubmissionId = message.SubmissionId,
            Title = message.Title,
            Body = message.Body,
            CreatedAt = message.OccurredAtUtc
        };

        await hubContext.Clients.Group(NotificationHub.UserGroup(message.StudentUserId)).SendAsync(NotificationHub.ReceiveMethod, payload, context.CancellationToken);

        logger.LogInformation("Pushed submission-graded notification to SignalR group for user {UserId}, submission {SubmissionId}.", message.StudentUserId, message.SubmissionId);
    }
}
