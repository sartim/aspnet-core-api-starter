# Reliability trends and quarterly SLO review

The **Reliability trend report** runs at the start of each quarter and can be
started manually. It compares the current 90-day period with the preceding
90-day period for each configured environment:

- availability error ratio against the 99.9% starting SLO;
- average request latency; and
- percentage change from the previous period.

The JSON and Markdown reports are uploaded as workflow artifacts. Missing
Prometheus configuration skips an environment; configure the same
`STAGING_PROMETHEUS_URL`, `PRODUCTION_PROMETHEUS_URL`, and optional
`PROMETHEUS_BEARER_TOKEN` used by SLO burn-rate reporting.

## Quarterly review checklist

1. Download the trend reports and compare error ratio, latency, traffic, and
   burn-rate artifacts with the previous quarter.
2. Overlay `deployment-annotation.json` events and identify releases or
   dependency changes near material trend shifts.
3. Review incidents, drift issues, synthetic-monitoring issues, and rollback
   readiness during the period.
4. Confirm SLO scope, exclusions, alert thresholds, and data quality. Do not
   raise the target solely to silence alerts.
5. Record capacity, database, cache, email, messaging, and external-adapter
   risks; assign owners and due dates for follow-up work.
6. Close the quarterly review issue only after the report, decisions, and
   follow-up tasks are linked.

The report status is `ok` when no review threshold is crossed, `review` when
error ratio or trend deterioration is material, and `critical` when the change
is severe enough to require incident follow-up. These are starting thresholds;
service owners should tune them with real traffic and error-budget policy.
