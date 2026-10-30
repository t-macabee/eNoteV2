using eNote.Contracts.Communication;
using eNote.Infrastructure.Messaging;
using eNote.Worker.Extensions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace eNote.Tests.Worker;

public sealed class WorkerConsumersTests
{
    public static IEnumerable<object[]> MessageTypes() =>
        NotificationMessageTypes.All.Select(type => new object[] { type.MessageType });

    [Theory]
    [MemberData(nameof(MessageTypes))]
    public void EveryNotificationMessage_HasAConsumer_AndAFaultConsumer(Type messageType)
    {
        var services = new ServiceCollection();
        services.AddMassTransitTestHarness(WorkerConsumers.Add);

        var consumerType = typeof(IConsumer<>).MakeGenericType(messageType);
        var faultConsumerType = typeof(IConsumer<>).MakeGenericType(typeof(Fault<>).MakeGenericType(messageType));
        var faultLoggingType = typeof(FaultLoggingConsumer<>).MakeGenericType(messageType);

        Assert.Contains(services, descriptor =>
            consumerType.IsAssignableFrom(descriptor.ServiceType) && descriptor.ServiceType != faultLoggingType);
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == faultLoggingType && faultConsumerType.IsAssignableFrom(descriptor.ServiceType));
    }
}
