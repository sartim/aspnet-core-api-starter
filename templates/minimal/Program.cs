var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddJsonConsole();
builder.Services.AddSingleton<StarterMetrics>();

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var port) ? port : 5070);
});

var app = builder.Build();
app.UseMiddleware<ObservabilityMiddleware>();

app.MapGet("/api/v1/health", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }));
app.MapGet("/metrics", (StarterMetrics metrics) => Results.Text(metrics.ToPrometheus(), "text/plain; version=0.0.4"));
app.Run();
