# ASP.NET API starter

A production-oriented ASP.NET Core API starter with PostgreSQL, Entity Framework Core, JWT authentication, and a Users/RBAC service profile.

This repository is named `aspnet-core-api-starter` and follows the same generated-profile model as the Drogon API starter.

## Generate a fresh API

Use the generator when starting a new project. The destination must be new or empty:

```bash
./scripts/aspnet-starter init shop-api ../shop-api
```

The default `user-service` profile includes the maintained Users/RBAC implementation, JWT authentication, EF Core migrations, Docker support, and tests. For a clean ASP.NET foundation without user-service:

```bash
./scripts/aspnet-starter init shop-api ../shop-api --profile minimal
```

Supported profiles:

* `user-service` (default): Users, roles, permissions, JWT authentication, PostgreSQL, migrations, and tests
* `minimal`: dependency-free ASP.NET Core API foundation with a health endpoint and baseline logging/tracing/metrics; no users, roles, permissions, EF Core, database, or authentication

The source repository itself is the complete `user-service` profile. The minimal profile lives in `templates/minimal` and is intentionally independent of the user-service implementation.

Both profiles require `PORT` at startup and report the exact missing or invalid
setting. The `user-service` profile additionally requires `DB_URL`,
`JWT_SECRET_KEY` (at least 32 bytes), `JWT_ISSUER`, and `JWT_AUDIENCE`.

Observability is part of the starter contract: logging, tracing, and metrics
work by default, while Sentry error tracking is enabled only when
`SENTRY_DSN` is configured and remains disabled without blocking startup.

The `/metrics` endpoint exposes low-cardinality Prometheus-compatible counters,
and responses include an `X-Trace-Id` correlation header.

Errors use RFC 9457-style `application/problem+json` responses with `status`,
`title`, `detail`, `instance`, and a `traceId` extension. Details are safe for
clients while logs retain exceptions for diagnostics.

### Observability configuration

The starter uses OpenTelemetry Protocol (OTLP) for vendor-neutral trace and
metric export. Set `OTEL_EXPORTER_OTLP_ENDPOINT` to an OTLP-compatible
collector or APM gateway, for example `http://localhost:4317`. The included
`docker-compose.otel.yaml` overlay starts a local collector for verification:

```bash
docker compose -f docker-compose.yaml -f docker-compose.otel.yaml up -d --build
```

Sentry is an optional error-tracking adapter. Set `SENTRY_DSN` to enable it;
an unset or invalid value falls back to the standard logging reporter without
blocking startup. The `IErrorReporter` abstraction can be replaced by another
error tracker or APM adapter without changing request error handling.

Telemetry must not include passwords, JWTs, database credentials, or
unnecessary personal data. Keep route labels and metric dimensions
low-cardinality, and configure the collector or backend to enforce retention,
access control, and environment-specific sampling.

See the [project roadmap](docs/ROADMAP.md) for milestones and task priorities.
See the [release checklist](docs/RELEASE_CHECKLIST.md) before production
deployments.
Existing release images can be promoted through the **Promote release** GitHub
Actions workflow. Staging accepts SemVer prereleases; production accepts only
stable SemVer versions and requires protected-environment approval.
See the [architecture and extension points](docs/ARCHITECTURE.md) when adapting
the starter to another identity provider, database, API design, or telemetry
backend.
See the [observability guide](docs/OBSERVABILITY.md) for optional OTLP export,
the replaceable error reporter, and the importable Grafana dashboard.
See the [email actions guide](docs/EMAIL_ACTIONS.md) for optional password-reset
and email-verification flows with a provider-neutral sender boundary.
See the [messaging guide](docs/MESSAGING.md) for optional broker adapters and
resource-change events without adding broker dependencies.
See [template versioning](docs/TEMPLATE_VERSIONING.md) for generated metadata
and the migration path between starter releases.
See [background jobs and outbox](docs/OUTBOX.md) for the opt-in durable event
processor.
See [integration adapters](docs/INTEGRATION_ADAPTERS.md) for contract-tested
HTTP reference implementations.
See [performance and SLO guidance](docs/PERFORMANCE_SLO.md) for the CI smoke
test and production measurement baseline.
See the [database migration runbook](docs/DATABASE_MIGRATIONS.md) for generated
projects and production deployment sequencing.

The Markdown documentation is published automatically to
[GitHub Pages](https://sartim.github.io/aspnet-core-api-starter/) from the
`main` branch with MkDocs Material by GitHub Actions.

Docker Compose is the included deployment baseline. Kubernetes manifests and
Helm charts are intentionally not maintained by this starter; teams can choose
their preferred orchestration platform and create those deployment assets for
their environment.

---

## Requirements

* .NET SDK **8.0+**
* PostgreSQL **14+**
* Docker & Docker Compose (optional, for containerized setup)

---

## Project Setup

### 1. Install Entity Framework Core CLI

```bash
dotnet tool install --global dotnet-ef
```

Verify installation:

```bash
dotnet ef --version
```

---

### 2. Environment Variables

Create a `.env` file by copying `.env.example` from the repository root.

#### `.env.example` contents

```env
ENV=Development
PORT=5070

# JWT
JWT_SECRET_KEY=CHANGE_ME_TO_A_SECURE_32_BYTE_MIN_SECRET
JWT_ISSUER=asp-shop-api
JWT_AUDIENCE=asp-shop-client
JWT_EXPIRY=300
JWT_REFRESH_EXPIRY=604800
AUTH_MAX_FAILED_ATTEMPTS=5
AUTH_LOCKOUT_MINUTES=15

# Optional error tracking
SENTRY_DSN=

# Optional vendor-neutral APM export (OTLP)
OTEL_EXPORTER_OTLP_ENDPOINT=

# Database
POSTGRES_USER=shopuser
POSTGRES_PASSWORD=shoppassword
POSTGRES_DB=shopdb

DB_URL=Host=postgres;Port=5432;Database=shopdb;Username=shopuser;Password=shoppassword
REDIS_CONNECTION=
```

> ⚠️ **Important**
>
> * `JWT_SECRET_KEY` must be **at least 32 bytes (256 bits)** for HMAC-SHA256
> * Generate one with:
> ```bash
>   openssl rand -base64 32
>   ```
> * Never commit `.env` files to version control

---

## Database Setup

### 3. Create a Migration

```bash
dotnet ef migrations add <MigrationName>
```

Example:

```bash
dotnet ef migrations add InitialCreate
```

---

### 4. Apply Migrations

```bash
dotnet ef database update
```

---

## Running the Application

### 5. Build the Project

```bash
dotnet build
```

---

### 6. Run Locally (with Hot Reload)

```bash
dotnet watch run -- --logging:LogLevel:Default=Debug
```

The API will be available at:

```
http://localhost:5070
```

Swagger UI:

```
http://localhost:5070/swagger
```

---

## Running with Docker

### 7. Start the Application Using Docker Compose

```bash
docker compose up -d --build
```

This will:

* Start PostgreSQL
* Run database migrations
* Launch the API container

To stop containers:

```bash
docker compose down
```

---

## Authentication

* Authentication is handled using **JWT tokens**
* Tokens are issued via the `/auth/token` endpoint
* Protected endpoints require:

```http
Authorization: Bearer <JWT_TOKEN>
```

### Non-interactive administrator creation

After the database is available, create the initial administrator without a
prompt. The command applies pending migrations, is safe to rerun, and accepts
flags or environment variables for deployment automation:

```bash
ADMIN_EMAIL=admin@example.com \
ADMIN_PASSWORD='change-this-in-a-secret-manager' \
dotnet run -- --create-admin
```

Optional values are `ADMIN_FIRST_NAME`, `ADMIN_LAST_NAME`, and `ADMIN_PHONE`.
The equivalent flags are `--admin-email`, `--admin-password`,
`--admin-first-name`, `--admin-last-name`, and `--admin-phone`.

---

## Development Notes

* Passwords are hashed using **BCrypt**
* Database access is handled via **Entity Framework Core**
* Controllers inherit from a shared `BaseController<T>` for common CRUD operations

---

## Common Issues

### JWT Key Error (`IDX10720`)

If you see:

```
key size must be greater than 256 bits
```

Your `JWT_SECRET_KEY` is too short. Generate a new one:

```bash
openssl rand -base64 32
```

---

## License

[MIT](https://choosealicense.com/licenses/mit/)

---
