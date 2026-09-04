using System.Net;
using System.Net.Http.Json;
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
        var body = await handler.Request!.Content!.ReadFromJsonAsync<Dictionary<string, string>>();
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

        var body = await handler.Request!.Content!.ReadFromJsonAsync<ResourceChangedEvent>();
        Assert.Equal("User", body?.ResourceType);
        Assert.Equal("created", body?.Operation);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
        }
    }
}
