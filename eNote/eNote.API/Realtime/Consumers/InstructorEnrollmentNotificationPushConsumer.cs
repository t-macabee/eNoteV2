using eNote.API.Hubs;
using eNote.Application.Features.Communication.Notifications;
using eNote.Contracts.Enrollments;
using Microsoft.AspNetCore.SignalR;

namespace eNote.API.Realtime.Consumers;

public sealed class InstructorEnrollmentNotificationPushConsumer(IHubContext<NotificationHub> hubContext, ILogger<InstructorEnrollmentNotificationPushConsumer> logger)
    : NotificationPushConsumer<InstructorEnrollmentNotification>(hubContext, logger)
{
    protected override NotificationPush Map(InstructorEnrollmentNotification message) => new(
        message.InstructorUserId,
        new NotificationPushDto
        {
            Title = message.Title,
            Body = message.Body,
            CourseId = message.CourseId,
            CreatedAt = message.OccurredAtUtc
        },
        "instructor-enrollment",
        $"enrollment {message.EnrollmentId}");
}
