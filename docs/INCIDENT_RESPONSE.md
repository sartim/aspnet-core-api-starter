# Incident response

Use this runbook with the environment dashboard and alert rules in
`observability/`. The dashboard is intentionally environment-filtered, while
the starter remains vendor-neutral: Prometheus/Grafana can be replaced by any
system that can consume the existing metrics, logs, traces, and health probes.

## First five minutes

1. Identify the environment, start time, affected endpoint, and alert severity.
2. Open the environment dashboard and compare request rate, errors, latency,
   readiness, and the latest deployment record.
3. Capture a trace ID from a problem-details response or structured log. Do not
   copy tokens, passwords, request bodies, or personal data into the incident.
4. Check the **Deployment drift monitor** and **Verify promoted release** runs.
5. Declare an incident for sustained critical errors, failed readiness, or
   suspected data loss; assign an incident lead and a communications owner.

## High error rate

Check whether the increase is isolated to one environment, route, or release.
Use correlated trace IDs to distinguish application exceptions from dependency
failures. Check PostgreSQL, Redis, email, messaging, and external HTTP adapter
health before changing application configuration.

If the incident began after a release and the rollback image passed the
verification workflow, use **Promote release** to promote the known-good image,
then run **Verify promoted release** and **Record deployment**. Keep the
database migration compatibility guidance in the release checklist in view;
application rollback is not a substitute for database recovery.

## High latency

Compare traffic volume, average latency, traces, database timings, external
adapter timings, and worker/outbox backlog. Confirm that the alert is not caused
by a low-traffic denominator. Use the performance SLO smoke test only as a
regression guard, not as a production capacity measurement.

Reduce load or disable an optional integration only according to its documented
fail-open behavior. Do not increase timeouts blindly; record the change and
recheck error rate and readiness.

## Readiness failure

`/health/live` indicates process health; `/health/ready` includes database
readiness in the full profile. Check database connectivity, credentials,
connection saturation, migrations, and recent infrastructure changes. Keep the
service out of rotation until readiness is stable. If liveness also fails,
inspect container logs and restart policy before considering rollback.

## Drift or rollback-readiness failure

Treat a digest mismatch, missing rollback image, or invalid attestation as a
release-control incident. Do not deploy an unverified tag. Preserve the monitor
run URL, latest GitHub Deployment record, image digest, and rollback version;
then correct the registry/deployment record or promote the known-good image
through the protected workflow.

## Closeout

Record the timeline, impact, triggering change, dashboard evidence, trace IDs
with sensitive data removed, mitigation, and follow-up tasks. Close the
deduplicated `[deployment-drift]` issue only after the scheduled monitor passes
and the deployment record reflects the verified image.
