using System.Text.Json;
using AspNetCoreApiStarter.Data;
using AspNetCoreApiStarter.Messaging;
using AspNetCoreApiStarter.Models;
using Microsoft.EntityFrameworkCore;

namespace AspNetCoreApiStarter.Jobs;

public interface IOutboxMessageHandler
{
    Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default);
}

public sealed class NullOutboxMessageHandler(ILogger<NullOutboxMessageHandler> logger) : IOutboxMessageHandler
{
    public Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Outbox message {MessageId} was observed by the default handler; configure IOutboxMessageHandler for delivery", message.Id);
        return Task.CompletedTask;
    }
}

public sealed class OutboxEventPublisher(ApplicationDbContext db) : IEventPublisher
{
    public async Task PublishAsync(ResourceChangedEvent message, CancellationToken cancellationToken = default)
    {
        db.OutboxMessages.Add(new OutboxMessage
        {
            EventType = nameof(ResourceChangedEvent),
            Payload = JsonSerializer.Serialize(message),
            OccurredAt = message.OccurredAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}

public sealed class OutboxProcessor(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessor> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessBatchAsync(stoppingToken);
            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var handler = scope.ServiceProvider.GetRequiredService<IOutboxMessageHandler>();
        var messages = await db.OutboxMessages.Where(message => message.ProcessedAt == null)
            .OrderBy(message => message.OccurredAt).Take(20).ToListAsync(cancellationToken);
        foreach (var message in messages)
        {
            try
            {
                message.Attempts++;
                await handler.HandleAsync(message, cancellationToken);
                message.ProcessedAt = DateTime.UtcNow;
                message.LastError = null;
            }
            catch (Exception exception)
            {
                message.LastError = exception.Message;
                logger.LogWarning(exception, "Outbox message {MessageId} failed on attempt {Attempt}", message.Id, message.Attempts);
            }
            message.UpdatedAt = DateTime.UtcNow;
        }
        if (messages.Count > 0)
            await db.SaveChangesAsync(cancellationToken);
    }
}
