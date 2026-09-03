using AspNetCoreApiStarter.Observability;

namespace AspNetCoreApiStarter.Tests.Observability;

public class StarterMetricsTests
{
    [Fact]
    public void TelemetryOptions_DisableExportWhenEndpointIsMissingOrInvalid()
    {
        var missing = StarterTelemetryOptions.FromEnvironment(new ConfigurationBuilder().Build());
        var invalid = StarterTelemetryOptions.FromEnvironment(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "collector:4317"
            })
            .Build());

        Assert.Null(missing.OtlpEndpoint);
        Assert.Null(invalid.OtlpEndpoint);
    }

    [Fact]
    public void TelemetryOptions_UsesConfiguredHttpEndpointAndServiceName()
    {
        var options = StarterTelemetryOptions.FromEnvironment(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "https://collector.example.com:4318",
                ["OTEL_SERVICE_NAME"] = "catalog-api"
            })
            .Build());

        Assert.Equal("catalog-api", options.ServiceName);
        Assert.Equal("https://collector.example.com:4318/", options.OtlpEndpoint?.ToString());
    }

    [Fact]
    public void ToPrometheus_ContainsLowCardinalityRequestCounters()
    {
        var metrics = new StarterMetrics();

        metrics.RecordRequest(TimeSpan.FromMilliseconds(12), failed: false);
        metrics.RecordRequest(TimeSpan.FromMilliseconds(8), failed: true);

        var output = metrics.ToPrometheus();

        Assert.Contains("aspnet_starter_requests_total 2", output);
        Assert.Contains("aspnet_starter_errors_total 1", output);
        Assert.Contains("aspnet_starter_request_duration_milliseconds_total 20", output);
    }
}
