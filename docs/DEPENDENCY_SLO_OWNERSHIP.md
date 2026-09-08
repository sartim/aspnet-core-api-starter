# Dependency SLO ownership

`observability/slo/dependency-ownership.yml` is the starter's machine-readable
ownership catalog. Each enabled dependency should have one accountable owner,
an escalation route, an availability target, a latency target, an alert, and a
documented failure mode.

The default catalog treats PostgreSQL as required for `user-service`, while
Redis, email, and messaging are optional. Optional integrations should not
silently become release blockers: use their fail-open, queue, or outbox behavior
and alert at the dependency owner rather than paging the API owner for every
transient provider error.

At each quarterly SLO review:

1. Replace example team names with real owners and on-call routes.
2. Confirm each enabled dependency has a probe or telemetry signal matching its
   alert name.
3. Compare dependency error/latency trends with the API's SLO burn and capacity
   results.
4. Review timeout, retry, queue, and fail-open behavior for overload risks.
5. Record changes to targets, ownership, escalation, or dependency criticality
   in the release/reliability review issue.

Capacity reviews should include the dependency ownership catalog, fixture
volume, and result artifact. This makes it explicit whether a failure is caused
by API compute, PostgreSQL data/index volume, cache behavior, or an optional
integration.
