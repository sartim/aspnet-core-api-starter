using System.Net;
using System.Text.Json;
using AspNetCoreApiStarter.Email;
using AspNetCoreApiStarter.Integrations;
using AspNetCoreApiStarter.Messaging;
using Microsoft.Extensions.Configuration;

namespace AspNetCoreApiStarter.Tests.Integrations;

public class HttpAdaptersTests
{
    [Fact]
    public async Task HttpEmailSender_PostsContractWithoutLoggingSecrets()
    {
        var handler = new RecordingHandler();
        var configuration = new ConfigurationManager { ["EMAIL_FROM"] = "noreply@example.com", ["EMAIL_PROVIDER_API_KEY"] = "secret" };
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://email.example.com") };
        var sender = new HttpEmailSender(client, configuration);

        await sender.SendAsync("user@example.com", "Verify", "https://app.example.com/verify?token=one-time");

        Assert.Equal(HttpMethod.Post, handler.Request?.Method);
        Assert.Equal("Bearer secret", handler.Request?.Headers.Authorization?.ToString());
        var body = JsonSerializer.Deserialize<Dictionary<string, string>>(handler.Body!);
        Assert.Equal("user@example.com", body!["to"]);
        Assert.Equal("https://app.example.com/verify?token=one-time", body["actionUrl"]);
    }

    [Fact]
    public async Task HttpEventPublisher_PostsResourceChangedEvent()
    {
        var handler = new RecordingHandler();
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://events.example.com") };
        var publisher = new HttpEventPublisher(client, new ConfigurationManager());

        await publisher.PublishAsync(new ResourceChangedEvent("User", "created", "42", DateTime.UtcNow));

        var body = JsonSerializer.Deserialize<ResourceChangedEvent>(handler.Body!, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        Assert.Equal("User", body?.ResourceType);
        Assert.Equal("created", body?.Operation);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        }
    }
}
