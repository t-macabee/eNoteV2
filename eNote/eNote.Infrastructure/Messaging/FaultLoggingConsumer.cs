using MassTransit;
using Microsoft.Extensions.Logging;

namespace eNote.Infrastructure.Messaging;

public sealed class FaultLoggingConsumer<TMessage>(ILogger<FaultLoggingConsumer<TMessage>> logger) : IConsumer<Fault<TMessage>>
    where TMessage : class
{
    public Task Consume(ConsumeContext<Fault<TMessage>> context)
    {
        logger.LogError(
            "Message {MessageType} exhausted retries. Reason: {Reason}",
            typeof(TMessage).Name,
            context.Message.Exceptions.FirstOrDefault()?.Message);
        return Task.CompletedTask;
    }
}
