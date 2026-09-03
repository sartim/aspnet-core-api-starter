using AspNetCoreApiStarter.Data;
using AspNetCoreApiStarter.Jobs;
using AspNetCoreApiStarter.Messaging;
using AspNetCoreApiStarter.Tests.TestHelpers;

namespace AspNetCoreApiStarter.Tests.Jobs;

public class OutboxEventPublisherTests
{
    [Fact]
    public async Task PublishAsync_PersistsSerializedEvent()
    {
        await using ApplicationDbContext db = DbContextHelper.GetInMemoryDbContext(Guid.NewGuid().ToString());
        var publisher = new OutboxEventPublisher(db);
        var occurredAt = DateTime.UtcNow;

        await publisher.PublishAsync(new ResourceChangedEvent("User", "created", "42", occurredAt));

        var message = Assert.Single(db.OutboxMessages);
        Assert.Equal(nameof(ResourceChangedEvent), message.EventType);
        Assert.Contains("\"ResourceType\":\"User\"", message.Payload, StringComparison.Ordinal);
        Assert.Null(message.ProcessedAt);
    }
}
