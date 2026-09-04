using System.Net.Http.Headers;
using System.Net.Http.Json;
using AspNetCoreApiStarter.Email;
using AspNetCoreApiStarter.Messaging;

namespace AspNetCoreApiStarter.Integrations;

public sealed class HttpEmailSender(HttpClient client, IConfiguration configuration) : IEmailSender
{
    public async Task SendAsync(string recipient, string subject, string actionUrl, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "")
        {
            Content = JsonContent.Create(new
            {
                to = recipient,
                subject,
                actionUrl,
                from = configuration["EMAIL_FROM"]
            })
        };
        AddBearerToken(request, configuration["EMAIL_PROVIDER_API_KEY"]);
        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private static void AddBearerToken(HttpRequestMessage request, string? token)
    {
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}

public sealed class HttpEventPublisher(HttpClient client, IConfiguration configuration) : IEventPublisher
{
    public async Task PublishAsync(ResourceChangedEvent message, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "")
        {
            Content = JsonContent.Create(message)
        };
        var token = configuration["EVENT_PUBLISHER_API_KEY"];
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
