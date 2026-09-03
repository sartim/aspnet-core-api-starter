# ASP.NET Core API Starter Roadmap

This roadmap tracks the work required to make `aspnet-core-api-starter` a
reliable, reusable starting point. Priorities are reviewed whenever a
milestone is completed or the starter profiles change.

## Priority guide

- **P0 — release blocker:** required for a usable, trustworthy starter
- **P1 — next:** important for the next iteration or developer experience
- **P2 — later:** valuable improvements that do not block adoption

## Completed

- [x] Renamed the solution, application, test project, namespaces, and Docker
  references to `AspNetCoreApiStarter`.
- [x] Kept the complete Users/RBAC implementation as the default
  `user-service` profile.
- [x] Added the `minimal` profile without user-service, EF Core, authentication,
  database, or third-party package dependencies.
- [x] Added `scripts/aspnet-starter init` with profile selection and project-name
  substitution.
- [x] Documented profile selection and generated-project usage.
- [x] Added this roadmap and linked it from the project README.
- [x] Added generator smoke tests for both profiles and wired them into CI.
- [x] Verified hosted .NET restore, formatting, build, profile smoke tests,
  package vulnerability checks, and coverage test execution in CI.
- [x] Added a root `.env.example`, optional Compose environment override, and
  a CI validation step for the Docker Compose configuration.
- [x] Added dedicated secret-history and container-image security workflows;
  hosted results are green on the current PR.
- [x] Added and passed a hosted Docker Compose runtime smoke test covering
  migrations and the API health endpoint.
- [x] Extended the hosted runtime test to verify the migrated Users/RBAC schema
  directly in PostgreSQL.
- [x] Verified the Users/RBAC database-backed integration path in hosted CI,
  including migration, health, and schema assertions.
- [x] Added an optional OTLP Collector overlay and hosted export smoke test;
  backend-specific exporters remain configurable through the same OTLP endpoint.
- [x] Verified OTLP traces and metrics are received by the hosted collector.
- [x] Added colored MkDocs Material Markdown documentation publishing to GitHub
  Pages through GitHub Actions, including light/dark mode and callouts.
- [x] Added a versioned API reference with example requests for health,
  authentication, users, roles, permissions, and role-permission endpoints.
- [x] Standardized client errors as problem details and included trace IDs in
  error responses and structured request logs.
- [x] Added tagged-release CD that publishes immutable GHCR images with
  version/SHA tags, SBOM/provenance, image metadata artifacts, and a protected
  production environment gate.
- [x] Configured the production GitHub Environment with required reviewer
  approval, no administrator bypass, and a `main`-only deployment policy.
- [x] Documented extension points for profile generation, API controllers,
  authentication, persistence, observability, and startup configuration.
- [x] Added a release checklist covering migrations, secrets, Docker images,
  semantic versioning, compatibility, deployment, and rollback.

## Active priorities

Tasks are ordered by priority within each milestone. Work from the highest
priority incomplete task first.

## What’s next

1. **P2-2:** Add platform-neutral health/readiness separation for deployments that require a database.

## Deployment scope

Docker Compose is the supported deployment baseline included in this repository.
Kubernetes manifests and Helm charts are intentionally out of scope. They remain
an adopter choice, allowing each team to use its preferred platform, chart
conventions, and operational policies without coupling the starter to one
deployment target.

### P0 — Release blockers

Status: **Next**

- [x] **P0-1:** Restore and build both profiles in a clean .NET 8 CI
  environment.
- [x] **P0-2:** Add generator smoke tests that verify both generated projects
  build and that the minimal profile contains no user-service source or
  dependencies.
- [x] **P0-3:** Run the full Users/RBAC test suite in CI, including a
  database-backed integration test path.
- [x] **P0-4:** Verify Docker Compose from a clean checkout, including
  migrations and health checks.
- [x] **P0-5:** Add the complete pull-request CI pipeline: restore, build,
  test, `dotnet format --verify-no-changes`, analyzers, and coverage output.
- [x] **P0-6:** Add security gates for NuGet vulnerabilities, dependency review,
  CodeQL, secret scanning, and container-image scanning.
- [x] **P0-7:** Add `.github/dependabot.yml` for NuGet, GitHub Actions, and
  Docker updates, with grouped non-breaking updates and a controlled update
  cadence. GitHub requires this configuration under `.github` for automated
  version-update pull requests.
- [x] **P0-8:** Validate required startup configuration with actionable errors;
  the full profile requires `PORT`, `DB_URL`, and JWT settings, while minimal
  requires only `PORT` by design.
- [x] **P0-9:** Decide and document the supported semantic-versioning and
  branching policy, including prereleases and breaking changes.
- [x] **P0-10:** Add fail-open Sentry error tracking: enable it only when
  `SENTRY_DSN` is configured, and never prevent startup when it is absent or
  invalid.
- [x] **P0-11:** Add baseline observability to both profiles: structured
  logging, request correlation, distributed tracing, and low-cardinality
  metrics for request count, latency, and errors.
- [x] **P0-12:** Verify OTLP export against at least one collector and document
  backend configuration for common APM providers.

### P1 — Next iteration

Status: **Planned**

- [x] **P1-1:** Add a checked-in `.env.example` at the repository root and ensure
  the generated user-service profile receives a safe copy.
- [x] **P1-2:** Add a non-interactive command for creating the first administrator,
  suitable for local setup and deployment scripts.
- [x] **P1-3:** Provide consistent API documentation and example requests for
  health, authentication, users, roles, permissions, and role-permission endpoints.
- [x] **P1-4:** Add structured logging, request IDs, and consistent error responses.
- [x] **P1-5:** Add a release checklist covering migrations, secrets, Docker
  images, and backwards-compatible API changes.
- [x] **P1-6:** Add CD for tagged releases: build an immutable versioned Docker
  image, publish release artifacts, generate release notes, and deploy only
  through protected environments with approvals and rollback guidance.
- [x] **P1-7:** Add a release workflow that creates and pushes `vX.Y.Z` tags only
  after CI passes, then creates the GitHub Release and attaches the generated
  artifacts.
- [x] **P1-8:** Document observability configuration, including `SENTRY_DSN`,
  environment-specific log levels, trace propagation, metrics scraping, and
  privacy rules for personally identifiable information.

### P1 — Service foundation

- [x] **P1-9:** Extract authentication, authorization, persistence, and API concerns
  into clearly documented extension points.
- [x] **P1-10:** Add role and permission authorization policies to protected endpoints.
- [x] **P1-11:** Define password, token expiry, refresh, revocation, and account-lockout
  behavior explicitly.
- [x] **P1-12:** Add pagination, filtering, validation, and stable response contracts to
  collection endpoints.
- [x] **P1-13:** Add database migration guidance for generated projects and production
  deployments.

## Versioning decision

The preferred first candidates are:

- **[Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning):**
  a .NET/MSBuild-integrated option driven by `version.json`, producing
  SemVer-compatible versions and commit-based build metadata.
- **[MinVer](https://github.com/adamralph/minver):** a smaller tag-first option
  that derives the assembly and package versions directly from Git tags.

Evaluate both against the desired workflow, then standardize on one. The
selected tool must provide the version consistently to assemblies, Docker
image tags, release artifacts, and GitHub Releases. Tag creation and release
publishing should remain explicit CI/CD steps, with no release triggered from
an unreviewed pull request.

The Dependabot configuration should follow [GitHub's Dependabot
documentation](https://docs.github.com/en/code-security/dependabot/dependabot-version-updates/configuration-options-for-the-dependabot.yml-file),
and the source security workflow should use [GitHub
CodeQL](https://docs.github.com/en/code-security/code-scanning/codeql/codeql-code-scanning).

### P2 — Optional capabilities

- [x] **P2-1:** Add optional Redis caching without making it a minimal-profile
  dependency.
- [x] **P2-2:** Add platform-neutral health/readiness separation for deployments
  that require a database.
- [ ] **P2-3:** Add optional observability exporters and dashboards that remain
  disabled unless configured.
- [ ] **P2-4:** Add optional email/password-reset and email-verification modules.
- [ ] **P2-5:** Add optional gRPC or messaging adapters without changing the REST
  baseline.
- [ ] **P2-6:** Publish versioned starter templates and a migration path between profile
  versions.

## Observability contract

Both generated profiles should provide a useful baseline without requiring a
third-party account. Logging, tracing, and metrics must work with their default
configuration. Sentry is an optional error-tracking sink: `SENTRY_DSN` enables
it, while an unset DSN disables it without preventing startup or changing the
request path. Telemetry must avoid passwords, tokens, database credentials,
and unnecessary personal data.

## Definition of done

A roadmap item is complete when the implementation, tests, documentation, and
CI behavior are updated together. A milestone can move to complete only when
all P0 items in that milestone are resolved and the generated output has been
verified from a clean destination.
