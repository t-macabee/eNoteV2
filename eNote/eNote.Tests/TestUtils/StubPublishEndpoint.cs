using MassTransit;

namespace eNote.Tests.TestUtils;

public sealed class StubPublishEndpoint : IPublishEndpoint
{
    public List<object> Published { get; } = [];

    public Task Publish<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        Published.Add(message!);
        return Task.CompletedTask;
    }

    public Task Publish<T>(T message, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default) where T : class =>
        Publish(message, cancellationToken);

    public Task Publish<T>(T message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default) where T : class =>
        Publish(message, cancellationToken);

    public Task Publish(object message, CancellationToken cancellationToken = default) =>
        Publish(message, message?.GetType() ?? typeof(object), cancellationToken);

    public Task Publish(object message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default) =>
        Publish(message, cancellationToken);

    public Task Publish(object message, Type messageType, CancellationToken cancellationToken = default)
    {
        Published.Add(message!);
        return Task.CompletedTask;
    }

    public Task Publish(object message, Type messageType, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default) =>
        Publish(message, messageType, cancellationToken);

    public Task Publish<T>(object values, CancellationToken cancellationToken = default) where T : class
    {
        Published.Add(values!);
        return Task.CompletedTask;
    }

    public Task Publish<T>(object values, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default) where T : class =>
        Publish<T>(values, cancellationToken);

    public Task Publish<T>(object values, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default) where T : class =>
        Publish<T>(values, cancellationToken);

    public ConnectHandle ConnectPublishObserver(IPublishObserver observer) => new StubConnectHandle();

    private sealed class StubConnectHandle : ConnectHandle
    {
        public void Disconnect() { }
        public void Dispose() { }
    }
}
