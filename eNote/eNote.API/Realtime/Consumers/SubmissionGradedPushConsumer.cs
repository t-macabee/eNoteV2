using eNote.API.Hubs;
using eNote.Application.Features.Communication.Notifications;
using eNote.Contracts.Assignments;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace eNote.API.Realtime.Consumers;

public sealed class SubmissionGradedPushConsumer(IHubContext<NotificationHub> hubContext, ILogger<SubmissionGradedPushConsumer> logger)
    : NotificationPushConsumer<SubmissionGraded>(hubContext, logger)
{
    protected override NotificationPush Map(SubmissionGraded message) => new(
        message.StudentUserId,
        new NotificationPushDto
        {
            SubmissionId = message.SubmissionId,
            Title = message.Title,
            Body = message.Body,
            CreatedAt = message.OccurredAtUtc
        },
        "submission-graded",
        $"submission {message.SubmissionId}");
}
