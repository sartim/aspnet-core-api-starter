# Reference integration adapters

The full profile includes contract-tested HTTP reference adapters for the two
provider seams:

- `HttpEmailSender` posts `{ to, subject, actionUrl, from }` to an email API.
- `HttpEventPublisher` posts `ResourceChangedEvent` JSON to an event gateway.

They are not registered by default. Add them only when the corresponding
external service is available:

```csharp
builder.Services.AddHttpClient<IEmailSender, HttpEmailSender>(client =>
    client.BaseAddress = new Uri(Environment.GetEnvironmentVariable("EMAIL_PROVIDER_URL")!));
builder.Services.AddHttpClient<IEventPublisher, HttpEventPublisher>(client =>
    client.BaseAddress = new Uri(Environment.GetEnvironmentVariable("EVENT_PUBLISHER_URL")!));
```

Optional `EMAIL_PROVIDER_API_KEY` and `EVENT_PUBLISHER_API_KEY` values are sent
as Bearer credentials. The adapters use standard `HttpClient`, return failures
to the caller for retry policy decisions, and never log credentials or payloads.
Replace them with a broker SDK, SMTP client, or cloud-native client when that
better matches the deployment.
