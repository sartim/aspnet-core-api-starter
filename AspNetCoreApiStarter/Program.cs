using Microsoft.EntityFrameworkCore;
using AspNetCoreApiStarter.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using AspNetCoreApiStarter.Models;
using AspNetCoreApiStarter.Observability;
using AspNetCoreApiStarter.Authorization;
using AspNetCoreApiStarter.Health;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
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

// Validate required runtime configuration before wiring services so deployment
// failures identify the missing setting instead of surfacing as a vague error.
int port = GetRequiredPort();
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
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Required configuration 'DB_URL' is missing. Set DB_URL to a PostgreSQL connection string before starting the user-service profile.");

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

var redisConnection = Environment.GetEnvironmentVariable("REDIS_CONNECTION");
if (string.IsNullOrWhiteSpace(redisConnection))
{
    builder.Services.AddDistributedMemoryCache();
}
else
{
    builder.Services.AddStackExchangeRedisCache(options => options.Configuration = redisConnection);
}

// Convert url structure to lower case
builder.Services.AddRouting(options => options.LowercaseUrls = true);

// JWT authentication setup
var jwtSecret = GetRequiredSetting("JWT_SECRET_KEY");
if (Encoding.UTF8.GetByteCount(jwtSecret) < 32)
    throw new InvalidOperationException("Configuration 'JWT_SECRET_KEY' must be at least 32 bytes for HMAC-SHA256 signing. Generate one with 'openssl rand -base64 32'.");
var jwtIssuer = GetRequiredSetting("JWT_ISSUER");
var jwtAudience = GetRequiredSetting("JWT_AUDIENCE");

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
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.Administrator, policy =>
        policy.RequireAuthenticatedUser().RequireRole("Administrator"));
    AddPermissionPolicy(options, AuthorizationPolicies.UsersManage);
    AddPermissionPolicy(options, AuthorizationPolicies.RolesManage);
    AddPermissionPolicy(options, AuthorizationPolicies.PermissionsManage);
    AddPermissionPolicy(options, AuthorizationPolicies.RolePermissionsManage);
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
    !context.Request.Path.StartsWithSegments("/api/v1/auth/refresh") &&
    !context.Request.Path.StartsWithSegments("/api/v1/health") &&
    !context.Request.Path.StartsWithSegments("/metrics") &&
    !context.Request.Path.StartsWithSegments("/health/live") &&
    !context.Request.Path.StartsWithSegments("/health/ready"),
appBuilder =>
{
    appBuilder.UseMiddleware<TokenAuthenticationMiddleware>();
});

if (args.Contains("--create-admin"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    await db.Database.MigrateAsync();

    var email = GetOption(args, "--admin-email") ?? Environment.GetEnvironmentVariable("ADMIN_EMAIL");
    var password = GetOption(args, "--admin-password") ?? Environment.GetEnvironmentVariable("ADMIN_PASSWORD");
    var firstName = GetOption(args, "--admin-first-name") ?? Environment.GetEnvironmentVariable("ADMIN_FIRST_NAME") ?? "Administrator";
    var lastName = GetOption(args, "--admin-last-name") ?? Environment.GetEnvironmentVariable("ADMIN_LAST_NAME") ?? "User";
    var phoneValue = GetOption(args, "--admin-phone") ?? Environment.GetEnvironmentVariable("ADMIN_PHONE") ?? "0";

    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        throw new InvalidOperationException("--create-admin requires --admin-email and --admin-password, or ADMIN_EMAIL and ADMIN_PASSWORD.");
    if (!PasswordPolicy.IsValid(password))
        throw new InvalidOperationException(PasswordPolicy.Requirements());
    if (!int.TryParse(phoneValue, out var phone))
        throw new InvalidOperationException("ADMIN_PHONE or --admin-phone must be a valid integer.");

    var administratorRole = await db.Roles.SingleOrDefaultAsync(role => role.Name == "Administrator");
    if (administratorRole is null)
    {
        administratorRole = new Role
        {
            Name = "Administrator",
            Description = "Full access to the starter API",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Roles.Add(administratorRole);
        await db.SaveChangesAsync();
    }

    var existingUser = await db.Users.Include(user => user.UserRoles).SingleOrDefaultAsync(u => u.Email == email);
    if (existingUser is not null)
    {
        Console.WriteLine($"Administrator already exists: {email}");
        if (!existingUser.UserRoles.Any(userRole => userRole.RoleId == administratorRole.Id))
        {
            db.UserRoles.Add(new UserRole { User = existingUser, Role = administratorRole });
            await db.SaveChangesAsync();
        }
    }
    else
    {
        db.Users.Add(new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Phone = phone,
            Password = BCrypt.Net.BCrypt.HashPassword(password),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var createdUser = await db.Users.SingleAsync(user => user.Email == email);
        db.UserRoles.Add(new UserRole { User = createdUser, Role = administratorRole });
        await db.SaveChangesAsync();
        Console.WriteLine($"Administrator created: {email}");
    }

    return;
}

if (args.Contains("--create-user"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

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
static string? GetOption(string[] arguments, string option)
{
    var index = Array.IndexOf(arguments, option);
    return index >= 0 && index + 1 < arguments.Length ? arguments[index + 1] : null;
}

static string GetRequiredSetting(string name)
{
    var value = Environment.GetEnvironmentVariable(name);
    if (string.IsNullOrWhiteSpace(value))
        throw new InvalidOperationException($"Required configuration '{name}' is missing. Set {name} before starting the user-service profile.");

    return value;
}

static int GetRequiredPort()
{
    var rawPort = Environment.GetEnvironmentVariable("PORT");
    if (string.IsNullOrWhiteSpace(rawPort))
        throw new InvalidOperationException("Required configuration 'PORT' is missing. Set PORT to a value between 1 and 65535 before starting the API.");
    if (!int.TryParse(rawPort, out var port) || port is < 1 or > 65535)
        throw new InvalidOperationException($"Configuration 'PORT' must be a whole number between 1 and 65535. Received '{rawPort}'.");

    return port;
}

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

static void AddPermissionPolicy(AuthorizationOptions options, string permission)
{
    options.AddPolicy(permission, policy =>
        policy.RequireAuthenticatedUser().AddRequirements(new PermissionRequirement(permission)));
}

app.UseHttpsRedirection();
app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();
app.MapStarterMetrics();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
}).AllowAnonymous();

app.Run();
