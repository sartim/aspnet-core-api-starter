# Observability

The starter has a vendor-neutral baseline and optional integrations. Logging,
trace correlation, `/metrics`, and OpenTelemetry instrumentation work without
an external account or telemetry service.

## Optional exporters

The full `user-service` profile exports traces and metrics over OTLP only when
`OTEL_EXPORTER_OTLP_ENDPOINT` is set to an `http://` or `https://` endpoint.
An unset or invalid value disables export and does not prevent startup. Set
`OTEL_SERVICE_NAME` to override the default service name.

```dotenv
OTEL_SERVICE_NAME=catalog-api
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
```

The checked-in Compose overlay starts an OTLP collector for local verification.
Point its exporters at the APM, metrics, or tracing products selected by the
application owner. The application remains coupled to OTLP, not to a vendor
SDK.

Sentry is a replaceable `IErrorReporter` adapter. `SENTRY_DSN` enables the
built-in adapter; without it, errors are reported through structured logging.
Another error tracker or APM can implement `IErrorReporter` without changing
controllers or problem-details responses.

## Metrics dashboard

`observability/grafana/aspnet-starter-overview.json` is an importable Grafana
starter dashboard for Prometheus-compatible scraping of `/metrics`. It is an
optional artifact: no Grafana or Prometheus service is added to the default
Compose stack. Import it only when those services are part of the deployment.

Do not add user IDs, email addresses, tokens, request bodies, or credentials to
logs, trace attributes, or metric labels.
