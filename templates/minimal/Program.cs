var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var port) ? port : 5070);
});

var app = builder.Build();

app.MapGet("/api/v1/health", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }));
app.Run();
