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

Observability is part of the starter contract: logging, tracing, and metrics
work by default, while Sentry error tracking is enabled only when
`SENTRY_DSN` is configured and remains disabled without blocking startup.

The `/metrics` endpoint exposes low-cardinality Prometheus-compatible counters,
and responses include an `X-Trace-Id` correlation header.

See the [project roadmap](docs/ROADMAP.md) for milestones and task priorities.

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

# Optional error tracking
SENTRY_DSN=

# Optional vendor-neutral APM export (OTLP)
OTEL_EXPORTER_OTLP_ENDPOINT=

# Database
POSTGRES_USER=shopuser
POSTGRES_PASSWORD=shoppassword
POSTGRES_DB=shopdb

DB_URL=Host=postgres;Port=5432;Database=shopdb;Username=shopuser;Password=shoppassword
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
