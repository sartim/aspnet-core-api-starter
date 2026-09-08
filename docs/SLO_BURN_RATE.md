# SLO burn-rate reporting

The **SLO burn-rate report** workflow runs every 15 minutes and can be started
manually. Configure repository variables for the Prometheus API:

```text
STAGING_PROMETHEUS_URL=https://prometheus-staging.example.com
PRODUCTION_PROMETHEUS_URL=https://prometheus.example.com
```

If authentication is required, store `PROMETHEUS_BEARER_TOKEN` as a GitHub
Actions secret. The report uses the starter's request and error counters to
calculate availability error-budget burn for 1-hour and 6-hour windows against
the 99.9% starting SLO. Reports are uploaded as workflow artifacts. Missing
configuration skips a scheduled environment without creating noise.

Fast burn (1-hour rate at 14.4x) or sustained burn (6-hour rate at 6x) creates
one deduplicated `[slo-burn-rate]` issue per environment. These thresholds are
starting points; tune them to the service's traffic and error budget.

Every successful **Record deployment** run also uploads
`deployment-annotation.json`, a small vendor-neutral event containing the
environment, version, image digest, rollback version, and timestamp. Import
this JSON into Grafana annotations or an incident system to place release
changes on the same timeline as SLO burn.
