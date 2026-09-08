# Synthetic monitoring

The **Synthetic API monitoring** workflow runs hourly and can be started
manually for staging, production, or both. Configure repository variables:

```text
STAGING_API_BASE_URL=https://staging.example.com
PRODUCTION_API_BASE_URL=https://api.example.com
SYNTHETIC_PROFILE=full
```

The monitor checks `/health/live`, `/health/ready`, `/metrics`, and the full
profile compatibility endpoint `/api/v1/health`. Minimal-profile projects skip
the compatibility endpoint when `profile=minimal` is selected. Health requests
must expose the correlation `X-Trace-Id` header, and metrics must include the
starter request counter.

Failures create one deduplicated `[synthetic-monitoring]` issue per environment.
Use the [incident response runbook](INCIDENT_RESPONSE.md) to triage the issue;
do not put credentials, tokens, request bodies, or personal data in URLs or
issue descriptions.

## Dependency probes

`observability/prometheus/aspnet-starter-dependency-alerts.yml` contains
optional rules for blackbox or platform probes labeled with `environment`,
`service`, and `dependency`. Import only the rules for dependencies actually
used by the generated project. Redis, email, and messaging alerts are warnings
because those adapters are optional and may be configured fail-open; PostgreSQL
readiness is critical for the full profile.
