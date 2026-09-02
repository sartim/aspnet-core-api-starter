using AspNetCoreApiStarter.Observability;

namespace AspNetCoreApiStarter.Tests.Observability;

public class StarterMetricsTests
{
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
