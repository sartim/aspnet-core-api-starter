using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Sentry;

namespace AspNetCoreApiStarter.Observability;

public interface IErrorReporter
{
    void Capture(Exception exception, HttpContext context);
}

public static class StarterProblemDetails
{
    public static string GetTraceId(HttpContext context)
        => Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;

    public static async Task WriteAsync(HttpContext context, int statusCode,
        string title, string detail, CancellationToken cancellationToken = default)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };
        problem.Extensions["traceId"] = GetTraceId(context);
        await context.Response.WriteAsJsonAsync(problem, cancellationToken);
    }
}

public sealed class LoggingErrorReporter(ILogger<LoggingErrorReporter> logger) : IErrorReporter
{
    public void Capture(Exception exception, HttpContext context)
    {
        logger.LogError(exception, "Unhandled request exception for {Method} {Path}",
            context.Request.Method, context.Request.Path);
    }
}

public sealed class SentryErrorReporter : IErrorReporter
{
    public void Capture(Exception exception, HttpContext context)
    {
        SentrySdk.CaptureException(exception, scope =>
        {
            scope.SetTag("trace_id", Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier);
        });
    }
}

public sealed class StarterMetrics
{
    public const string MeterName = "AspNetCoreApiStarter";
    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> RequestCounter = Meter.CreateCounter<long>("aspnet_starter.requests");
    private static readonly Counter<long> ErrorCounter = Meter.CreateCounter<long>("aspnet_starter.errors");
    private static readonly Histogram<double> DurationHistogram = Meter.CreateHistogram<double>("aspnet_starter.request.duration", "ms");
    private long _requests;
    private long _errors;
    private long _durationMilliseconds;

    public void RecordRequest(TimeSpan duration, bool failed)
    {
        Interlocked.Increment(ref _requests);
        if (failed) Interlocked.Increment(ref _errors);
        Interlocked.Add(ref _durationMilliseconds, (long)duration.TotalMilliseconds);
        RequestCounter.Add(1);
        if (failed) ErrorCounter.Add(1);
        DurationHistogram.Record(duration.TotalMilliseconds);
    }

    public string ToPrometheus()
    {
        var requests = Interlocked.Read(ref _requests);
        var errors = Interlocked.Read(ref _errors);
        var duration = Interlocked.Read(ref _durationMilliseconds);
        var builder = new StringBuilder();
        builder.AppendLine("# TYPE aspnet_starter_requests_total counter");
        builder.AppendLine($"aspnet_starter_requests_total {requests}");
        builder.AppendLine("# TYPE aspnet_starter_errors_total counter");
        builder.AppendLine($"aspnet_starter_errors_total {errors}");
        builder.AppendLine("# TYPE aspnet_starter_request_duration_milliseconds_total counter");
        builder.AppendLine($"aspnet_starter_request_duration_milliseconds_total {duration}");
        return builder.ToString();
    }
}

public sealed class StarterExceptionHandler(
    ILogger<StarterExceptionHandler> logger,
    IErrorReporter errorReporter) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        errorReporter.Capture(exception, httpContext);
        var traceId = StarterProblemDetails.GetTraceId(httpContext);
        logger.LogError(exception, "Unhandled request exception for {Method} {Path} with trace ID {TraceId}",
            httpContext.Request.Method, httpContext.Request.Path, traceId);

        if (httpContext.Response.HasStarted)
            return false;

        await StarterProblemDetails.WriteAsync(httpContext,
            StatusCodes.Status500InternalServerError,
            "Internal Server Error",
            "An unexpected error occurred. Use the traceId when contacting support.",
            cancellationToken);
        return true;
    }
}

public sealed class StarterObservabilityMiddleware(
    RequestDelegate next,
    StarterMetrics metrics,
    ILogger<StarterObservabilityMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var started = Stopwatch.GetTimestamp();
        var failed = false;
        var traceId = StarterProblemDetails.GetTraceId(context);
        context.Response.Headers.TryAdd("X-Trace-Id",
            traceId);

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
            logger.LogInformation("HTTP {Method} {Path} completed with {StatusCode} in {DurationMs}ms with trace ID {TraceId}",
                context.Request.Method, context.Request.Path, context.Response.StatusCode,
                duration.TotalMilliseconds, traceId);
        }
    }
}

public static class ObservabilityExtensions
{
    public static IServiceCollection AddStarterObservability(this IServiceCollection services, string? sentryDsn = null)
    {
        services.AddSingleton<StarterMetrics>();
        if (string.IsNullOrWhiteSpace(sentryDsn))
            services.AddSingleton<IErrorReporter, LoggingErrorReporter>();
        else
            services.AddSingleton<IErrorReporter, SentryErrorReporter>();
        services.AddExceptionHandler<StarterExceptionHandler>();
        services.AddProblemDetails();
        return services;
    }

    public static IApplicationBuilder UseStarterObservability(this IApplicationBuilder app)
        => app.UseMiddleware<StarterObservabilityMiddleware>();

    public static IEndpointRouteBuilder MapStarterMetrics(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/metrics", (StarterMetrics metrics) =>
            Results.Text(metrics.ToPrometheus(), "text/plain; version=0.0.4"));
        return endpoints;
    }
}
