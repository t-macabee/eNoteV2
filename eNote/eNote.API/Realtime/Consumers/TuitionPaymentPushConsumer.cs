using eNote.API.Hubs;
using eNote.Application.Features.Communication.Notifications;
using eNote.Contracts.Enrollments;
using Microsoft.AspNetCore.SignalR;

namespace eNote.API.Realtime.Consumers;

public sealed class TuitionPaymentPushConsumer(IHubContext<NotificationHub> hubContext, ILogger<TuitionPaymentPushConsumer> logger)
    : NotificationPushConsumer<TuitionPaymentNotification>(hubContext, logger)
{
    protected override NotificationPush Map(TuitionPaymentNotification message) => new(
        message.StudentUserId,
        new NotificationPushDto
        {
            Title = message.Title,
            Body = message.Body,
            CreatedAt = message.OccurredAtUtc
        },
        "tuition-payment",
        $"enrollment {message.EnrollmentId}");
}
