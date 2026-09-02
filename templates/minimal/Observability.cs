using System.Diagnostics;
using System.Text;

public sealed class StarterMetrics
{
    private long _requests;
    private long _errors;
    private long _durationMilliseconds;

    public void RecordRequest(TimeSpan duration, bool failed)
    {
        Interlocked.Increment(ref _requests);
        if (failed) Interlocked.Increment(ref _errors);
        Interlocked.Add(ref _durationMilliseconds, (long)duration.TotalMilliseconds);
    }

    public string ToPrometheus()
    {
        var builder = new StringBuilder();
        builder.AppendLine("# TYPE aspnet_starter_requests_total counter");
        builder.AppendLine($"aspnet_starter_requests_total {Interlocked.Read(ref _requests)}");
        builder.AppendLine("# TYPE aspnet_starter_errors_total counter");
        builder.AppendLine($"aspnet_starter_errors_total {Interlocked.Read(ref _errors)}");
        builder.AppendLine("# TYPE aspnet_starter_request_duration_milliseconds_total counter");
        builder.AppendLine($"aspnet_starter_request_duration_milliseconds_total {Interlocked.Read(ref _durationMilliseconds)}");
        return builder.ToString();
    }
}

public sealed class ObservabilityMiddleware(RequestDelegate next, StarterMetrics metrics, ILogger<ObservabilityMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var started = Stopwatch.GetTimestamp();
        var failed = false;
        context.Response.Headers.TryAdd("X-Trace-Id", Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier);
        try
        {
            await next(context);
            failed = context.Response.StatusCode >= 500;
        }
        catch
        {
            failed = true;
            throw;
        }
        finally
        {
            var duration = Stopwatch.GetElapsedTime(started);
            metrics.RecordRequest(duration, failed);
            logger.LogInformation("HTTP {Method} {Path} completed with {StatusCode} in {DurationMs}ms",
                context.Request.Method, context.Request.Path, context.Response.StatusCode, duration.TotalMilliseconds);
        }
    }
}
