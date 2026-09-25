using eNote.API.Hubs;
using eNote.Application.Features.Communication.Notifications;
using eNote.Contracts.Enrollments;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace eNote.API.Realtime.Consumers;

public sealed class EnrollmentStatusChangedPushConsumer(IHubContext<NotificationHub> hubContext, ILogger<EnrollmentStatusChangedPushConsumer> logger)
    : NotificationPushConsumer<EnrollmentStatusChanged>(hubContext, logger)
{
    protected override NotificationPush Map(EnrollmentStatusChanged message) => new(
        message.StudentUserId,
        new NotificationPushDto
        {
            Title = message.Title,
            Body = message.Body,
            CreatedAt = message.OccurredAtUtc
        },
        "enrollment-status-changed",
        $"enrollment {message.EnrollmentId}");
}
