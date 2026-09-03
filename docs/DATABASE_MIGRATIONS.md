# Database migrations

The full `user-service` profile uses Entity Framework Core migrations against
PostgreSQL. Generated projects receive the same migration history as the
source profile and should treat schema changes as reviewed release artifacts.
The `minimal` profile has no database or migration dependency.

## Development workflow

From the generated project directory, install the EF CLI once and inspect the
current migration history:

```bash
dotnet tool install --global dotnet-ef --version 8.0.10
dotnet ef migrations list \
  --project AspNetCoreApiStarter/AspNetCoreApiStarter.csproj \
  --startup-project AspNetCoreApiStarter/AspNetCoreApiStarter.csproj
```

After changing an entity or `OnModelCreating`, create a descriptive migration:

```bash
dotnet ef migrations add AddCatalogIndexes \
  --project AspNetCoreApiStarter/AspNetCoreApiStarter.csproj \
  --startup-project AspNetCoreApiStarter/AspNetCoreApiStarter.csproj
```

Review the generated `Up` and `Down` methods, run the migration against a
disposable database, and exercise `/api/v1/health` before opening the PR.
Never edit an already-applied migration; add a new migration instead.

For this repository, the helper provides the same operations:

```bash
./scripts/aspnet-migrate list
./scripts/aspnet-migrate script /tmp/aspnet-api-migrations.sql
DB_URL='Host=localhost;Port=5432;Database=shopdb;Username=shopuser;Password=secret' \
  ./scripts/aspnet-migrate apply
```

`script` creates an idempotent SQL script and does not need a live database.
`apply` requires `DB_URL` and should be used only against the intended
database.

## Generated projects and Compose

The generator copies migrations into the default `user-service` profile. A
generated project should commit its migration files, update its release notes,
and run the migration helper from its own repository. Do not copy migrations
into the `minimal` profile.

Docker Compose uses a one-shot `migrate` service. It waits for PostgreSQL,
executes `dotnet ef database update`, and only then starts the API. This is the
supported local and smoke-test deployment baseline. It is safe to rerun after
the database volume is preserved because EF tracks applied migrations in
`__EFMigrationsHistory`.

## Production deployment

Apply schema changes as an explicit deployment step, before starting an image
that requires the new schema:

1. Back up the database and verify the restore point.
2. Review the migration for destructive operations, lock duration, and data
   conversion risk.
3. Generate and review an idempotent script with `aspnet-migrate script`.
4. Apply it once using the production migration runner or a controlled
   operator session, with `DB_URL` supplied by the secret manager.
5. Confirm the migration level, `/api/v1/health`, logs, metrics, and traces.
6. Deploy the application image and record the image digest plus migration
   level in the release checklist.

The API does not automatically migrate on ordinary startup. This prevents
every replica from racing to change production schema. The Compose one-shot
migration service and a protected CI/CD migration job are the appropriate
places to run schema changes.

Prefer expand-and-contract releases: add nullable or compatible schema first,
deploy code that writes both representations, backfill data, and remove the
old schema only in a later release. A `Down` migration is not a substitute for
a production backup or tested data recovery plan.
