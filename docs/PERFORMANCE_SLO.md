# Performance and SLO baseline

The runtime CI workflow runs a small performance smoke test against
`/health/live`. It sends 40 sequential requests and requires all responses to
be successful with a p95 latency below one second. This is a regression guard,
not a capacity test or a production SLO guarantee.

For production, define service-level objectives with real traffic and an
appropriate load tool. A useful starting point is:

- availability: 99.9% for API requests, excluding planned maintenance;
- latency: p95 below 500 ms and p99 below 1 s for representative read paths;
- errors: less than 0.1% HTTP 5xx responses;
- readiness: database readiness failures alert separately from process liveness.

Measure these objectives from the deployed service using the existing metrics,
traces, logs, and health probes. Use k6, Bombardier, NBomber, or a platform
load-testing service for sustained, concurrent tests; keep credentials and
personal data out of test payloads. Tune thresholds per environment rather than
loosening the CI smoke test to hide regressions.
