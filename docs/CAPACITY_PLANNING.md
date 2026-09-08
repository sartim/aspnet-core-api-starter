# Capacity planning

The **Capacity smoke test** workflow is manually triggered against a reachable
staging or production-like URL. It uses a pinned k6 image and checks both
`/health/live` and `/health/ready` under three deliberately small profiles:

| Scenario | Virtual users | Duration | p95 target | Error target |
| --- | ---: | ---: | ---: | ---: |
| `baseline` | 5 | 30s | <500 ms | <1% |
| `2x` | 10 | 60s | <750 ms | <1% |
| `5x` | 25 | 120s | <1 s | <2% |

These are comparison scenarios, not production load guarantees. Run them from
a network location representative of the target users, keep test payloads free
of personal data, and schedule heavier tests outside peak traffic. Record the
image version, database tier, cache configuration, traffic assumptions, and
results with the capacity review.

## Decision points

- If baseline fails, stop and fix the release or environment before scaling the
  scenario.
- If baseline passes but `2x` fails, inspect database saturation, connection
  pools, CPU, memory, and external dependency latency before increasing API
  replicas.
- If `5x` fails while dependencies remain healthy, record the limiting resource
  and plan a targeted scale or performance change.
- Repeat the scenario after material schema, caching, authentication, or
  integration changes.

Dependency ownership and target SLOs are recorded in
`observability/slo/dependency-ownership.yml`. Replace the example owner names
with the adopting organization's real teams and escalation routes.
