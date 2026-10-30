using eNote.Contracts.Communication;
using eNote.Infrastructure.Messaging;
using eNote.Worker.Consumers;
using MassTransit;

namespace eNote.Worker.Extensions;

public static class WorkerConsumers
{
    public static void Add(IBusRegistrationConfigurator bus)
    {
        bus.AddConsumer<RentalStatusChangedConsumer>();
        bus.AddConsumer<RentalStatusChangedEmailConsumer>();
        bus.AddConsumer<RentalRefundedConsumer>();
        bus.AddConsumer<LectureCancelledConsumer>();
        bus.AddConsumer<SubmissionGradedConsumer>();
        bus.AddConsumer<AnnouncementPublishedConsumer>();
        bus.AddConsumer<EnrollmentStatusChangedConsumer>();
        bus.AddConsumer<TuitionPaymentConsumer>();
        bus.AddConsumer<MembershipWarningConsumer>();
        bus.AddConsumer<MembershipWarningEmailConsumer>();
        bus.AddConsumer<PasswordResetEmailConsumer>();

        foreach (var (messageType, _) in NotificationMessageTypes.All)
        {
            bus.AddConsumer(typeof(FaultLoggingConsumer<>).MakeGenericType(messageType));
        }
    }
}
