# Messaging adapters

The full profile publishes a small `ResourceChangedEvent` contract after
successful user-service CRUD operations. Messaging is optional: the default
`NullEventPublisher` skips delivery, no broker package is installed, and a
publisher failure is logged without changing the REST response.

Replace the default registration in a generated application with an adapter for
the broker or transport selected by the team:

```csharp
builder.Services.AddSingleton<IEventPublisher, YourEventPublisher>();
```

The interface can be implemented with RabbitMQ, Kafka, Azure Service Bus,
Amazon SNS/SQS, Google Pub/Sub, or an internal queue. For durable delivery,
replace the no-op publisher with an outbox-backed implementation and make the
consumer idempotent. The starter deliberately does not impose broker
configuration, retry policy, serialization format, or deployment topology.

The minimal profile remains dependency-free and unchanged. REST endpoints and
their response contracts do not depend on a messaging backend.
