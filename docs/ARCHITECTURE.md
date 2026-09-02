# Extension points

The starter keeps the default profiles useful while leaving the main seams
replaceable. Generated applications can adopt these extension points without
changing the public profile contract.

## Profiles and generation

`user-service` is the default profile and includes PostgreSQL, EF Core, JWT
authentication, Users/RBAC, migrations, and Docker support. `minimal` is an
independent REST baseline with health, logging, tracing, metrics, and no
database or authentication dependencies.

Generate from the maintained source with:

```bash
./scripts/aspnet-starter init catalog-api ../catalog-api --profile minimal
```

The generator copies the selected profile, substitutes the project identifier,
and leaves the generated project independent of this repository.

## API layer

Controllers are the HTTP boundary. The full profile’s resource controllers
inherit from `BaseController<TEntity>` for shared CRUD behavior and the default
administrator authorization requirement. Replace the base controller or create
a dedicated controller when an endpoint needs custom validation, pagination,
response contracts, or authorization policies.

Keep transport DTOs separate from persistence models when the API becomes
public. This prevents database fields such as password hashes and soft-delete
metadata from becoming accidental response contracts.

## Authentication and authorization

`AuthController` issues JWTs and `TokenAuthenticationMiddleware` validates the
Bearer token. Replace the middleware or JWT configuration when integrating an
external identity provider, while preserving `HttpContext.User` claims for
downstream authorization.

Authorization is configured in `Program.cs`. Add named policies with
`AddAuthorization`, then apply `[Authorize(Policy = "your-policy")]` to endpoints. The
current starter includes the administrator role boundary; fine-grained
role-permission policies are the next service-foundation milestone.

## Persistence

`ApplicationDbContext` is the EF Core boundary. Add entities and indexes in
`OnModelCreating`, create migrations with `dotnet ef migrations add`, and apply
them through the deployment migration step. Replace `UseNpgsql` and the context
implementation when using another relational provider, keeping migrations and
connection handling environment-specific.

## Observability and errors

`AddStarterObservability` registers request metrics, the exception handler,
problem-details responses, and the `IErrorReporter` abstraction. The default
reporter logs errors; the optional Sentry adapter can be replaced with another
error tracker or APM without changing controllers.

OpenTelemetry tracing and metrics are configured separately in `Program.cs`.
Add provider exporters there or use the OTLP endpoint contract. Keep telemetry
dimensions low-cardinality and exclude passwords, tokens, credentials, and
unnecessary personal data.

## Configuration and startup

Required settings are validated before the application starts. Keep deployment
configuration outside source code and use the checked-in `.env.example` only
as a safe reference. Add new settings to that example, startup validation, and
the release checklist together.
