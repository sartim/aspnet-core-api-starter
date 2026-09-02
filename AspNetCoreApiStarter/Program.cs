using Microsoft.EntityFrameworkCore;
using AspNetCoreApiStarter.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using AspNetCoreApiStarter.Models;
using AspNetCoreApiStarter.Observability;
using Sentry;
using dotenv.net;
using OpenTelemetry.Resources;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

// Load .env
DotEnv.Load();

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddJsonConsole();
var sentryDsn = Environment.GetEnvironmentVariable("SENTRY_DSN");
var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
var hasOtlpEndpoint = Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out var parsedOtlpEndpoint);
if (!string.IsNullOrWhiteSpace(sentryDsn) &&
    Uri.TryCreate(sentryDsn, UriKind.Absolute, out var parsedSentryDsn) &&
    (parsedSentryDsn.Scheme == Uri.UriSchemeHttps || parsedSentryDsn.Scheme == Uri.UriSchemeHttp))
{
    builder.WebHost.UseSentry(options =>
    {
        options.Dsn = sentryDsn;
        options.SendDefaultPii = false;
    });
}
else if (!string.IsNullOrWhiteSpace(sentryDsn))
{
    Console.Error.WriteLine("SENTRY_DSN is invalid; Sentry error tracking is disabled.");
    sentryDsn = null;
}

// Set port for web server
int port = Convert.ToInt32(Environment.GetEnvironmentVariable("PORT"));
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(port);
});

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddStarterObservability(sentryDsn);
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("aspnet-core-api-starter"))
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation();
        if (hasOtlpEndpoint)
            tracing.AddOtlpExporter(options => options.Endpoint = parsedOtlpEndpoint!);
    })
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation();
        metrics.AddRuntimeInstrumentation();
        metrics.AddMeter(StarterMetrics.MeterName);
        if (hasOtlpEndpoint)
            metrics.AddOtlpExporter(options => options.Endpoint = parsedOtlpEndpoint!);
    });
if (!string.IsNullOrWhiteSpace(otlpEndpoint) && !hasOtlpEndpoint)
    Console.Error.WriteLine("OTEL_EXPORTER_OTLP_ENDPOINT is invalid; OTLP export is disabled.");

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Db connection here
var connectionString = Environment.GetEnvironmentVariable("DB_URL");
if (string.IsNullOrEmpty(connectionString))
    throw new Exception("DB_URL environment variable is not set.");

builder.Services.AddDbContext<ShopDbContext>(options => options.UseNpgsql(connectionString));

// Convert url structure to lower case
builder.Services.AddRouting(options => options.LowercaseUrls = true);

// JWT authentication setup
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") 
                ?? throw new Exception("JWT_SECRET_KEY not set.");
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") 
                ?? throw new Exception("JWT_ISSUER not set.");
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") 
                  ?? throw new Exception("JWT_AUDIENCE not set.");

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

//services.AddScoped(typeof(BaseController<>));
//services.AddScoped<UserController>();

var app = builder.Build();
app.UseExceptionHandler();
app.UseStarterObservability();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Skips auth check
app.UseWhen(context => 
    !context.Request.Path.StartsWithSegments("/api/v1/auth/generate-jwt") &&
    !context.Request.Path.StartsWithSegments("/api/v1/health") &&
    !context.Request.Path.StartsWithSegments("/metrics"),
appBuilder =>
{
    appBuilder.UseMiddleware<TokenAuthenticationMiddleware>();
});

if (args.Contains("--create-user"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();

    Console.WriteLine("=== Create Default User ===");

    Console.Write("First Name: ");
    string firstName = Console.ReadLine()!.Trim();

    Console.Write("Last Name: ");
    string lastName = Console.ReadLine()!.Trim();

    Console.Write("Email: ");
    string email = Console.ReadLine()!.Trim();

    Console.Write("Phone: ");
    int phone;
    while (!int.TryParse(Console.ReadLine(), out phone))
    {
        Console.Write("Invalid number. Phone: ");
    }

    Console.Write("Password: ");
    string password = ReadPassword();

    // Check if user already exists
    if (await db.Users.AnyAsync(u => u.Email == email))
    {
        Console.WriteLine("User with this email already exists.");
    }
    else
    {
        var user = new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Phone = phone,
            Password = BCrypt.Net.BCrypt.HashPassword(password),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        Console.WriteLine($"User created: {email}");
    }

    return; // exit after CLI run
}

// Helper: Hide password input
static string ReadPassword()
{
    string password = "";
    ConsoleKeyInfo key;

    do
    {
        key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Backspace && password.Length > 0)
        {
            password = password[..^1];
            Console.Write("\b \b");
        }
        else if (!char.IsControl(key.KeyChar))
        {
            password += key.KeyChar;
            Console.Write("*");
        }
    } while (key.Key != ConsoleKey.Enter);

    Console.WriteLine();
    return password;
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapStarterMetrics();

app.Run();
