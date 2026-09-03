var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddJsonConsole();
builder.Services.AddSingleton<StarterMetrics>();

var rawPort = Environment.GetEnvironmentVariable("PORT");
if (string.IsNullOrWhiteSpace(rawPort))
    throw new InvalidOperationException("Required configuration 'PORT' is missing. Set PORT to a value between 1 and 65535 before starting the minimal profile.");
if (!int.TryParse(rawPort, out var port) || port is < 1 or > 65535)
    throw new InvalidOperationException($"Configuration 'PORT' must be a whole number between 1 and 65535. Received '{rawPort}'.");

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(port);
});

var app = builder.Build();
app.UseMiddleware<ObservabilityMiddleware>();

app.MapGet("/api/v1/health", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }));
app.MapGet("/health/live", () => Results.Ok(new { status = "Healthy", check = "live", timestamp = DateTime.UtcNow }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "Healthy", check = "ready", timestamp = DateTime.UtcNow }));
app.MapGet("/metrics", (StarterMetrics metrics) => Results.Text(metrics.ToPrometheus(), "text/plain; version=0.0.4"));
app.Run();
