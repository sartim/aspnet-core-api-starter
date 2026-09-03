# Background jobs and durable outbox

The full profile includes an opt-in durable outbox for resource-change events.
It is disabled by default and does not add a broker dependency or alter REST
responses.

Enable it after applying database migrations:

```dotenv
OUTBOX_ENABLED=true
```

When enabled, successful CRUD events are stored in `OutboxMessages`. The
background processor polls pending messages, records attempts and failures,
and retries messages that remain unprocessed. Replace
`IOutboxMessageHandler` with a handler that publishes to the selected queue or
job system. Consumers must be idempotent because delivery is at-least-once.

The default handler only records that a message was observed; it is safe for
development but is not a production transport. For stronger transactional
guarantees, keep application writes and outbox insertion in the same database
transaction in the generated application's domain workflow.

The minimal profile remains dependency-free and has no background worker.
