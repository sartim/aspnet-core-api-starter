namespace AspNetCoreApiStarter.Messaging;

public sealed record ResourceChangedEvent(
    string ResourceType,
    string Operation,
    string ResourceId,
    DateTime OccurredAt);

public interface IEventPublisher
{
    Task PublishAsync(ResourceChangedEvent message, CancellationToken cancellationToken = default);
}

public sealed class NullEventPublisher(ILogger<NullEventPublisher> logger) : IEventPublisher
{
    public Task PublishAsync(ResourceChangedEvent message, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Messaging is not configured; skipped {Operation} event for {ResourceType} {ResourceId}",
            message.Operation, message.ResourceType, message.ResourceId);
        return Task.CompletedTask;
    }
}
