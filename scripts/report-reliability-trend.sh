#!/usr/bin/env bash
set -euo pipefail

usage() {
  echo "Usage: $0 <prometheus-url> <environment> [period-days] [json-path] [markdown-path]" >&2
  exit 2
}

prometheus_url=${1:-}
environment=${2:-}
period_days=${3:-90}
json_path=${4:-reliability-trend.json}
markdown_path=${5:-reliability-trend.md}
[[ -n "$prometheus_url" && -n "$environment" ]] || usage
[[ "$period_days" =~ ^[0-9]+$ && "$period_days" -ge 7 && "$period_days" -le 365 ]] || {
  echo "period-days must be between 7 and 365." >&2
  exit 1
}
prometheus_url="${prometheus_url%/}"

work_dir="$(mktemp -d)"
trap 'rm -rf "$work_dir"' EXIT
end_time="$(date -u +%s)"
current_start=$((end_time - period_days * 86400))
previous_start=$((current_start - period_days * 86400))

query_range() {
  local expression=$1
  local start=$2
  local end=$3
  local response
  if [[ -n "${PROMETHEUS_BEARER_TOKEN:-}" ]]; then
    response="$(curl --fail --silent --show-error --retry 3 --retry-all-errors --retry-delay 2 \
      --connect-timeout 5 --max-time 30 \
      -H "Authorization: Bearer ${PROMETHEUS_BEARER_TOKEN}" -G \
      --data-urlencode "query=${expression}" \
      --data-urlencode "start=${start}" --data-urlencode "end=${end}" \
      --data-urlencode "step=86400" "${prometheus_url}/api/v1/query_range")"
  else
    response="$(curl --fail --silent --show-error --retry 3 --retry-all-errors --retry-delay 2 \
      --connect-timeout 5 --max-time 30 -G \
      --data-urlencode "query=${expression}" \
      --data-urlencode "start=${start}" --data-urlencode "end=${end}" \
      --data-urlencode "step=86400" "${prometheus_url}/api/v1/query_range")"
  fi
  jq -e '.status == "success" and (.data.result | length) > 0' <<< "$response" >/dev/null || {
    echo "Prometheus returned no usable trend data for ${environment}: ${expression}" >&2
    return 1
  }
  printf '%s\n' "$response"
}

summarize() {
  jq -r '[.data.result[0].values[]?[1] | tonumber] | if length == 0 then 0 else add / length end' "$1"
}

error_expression="sum(rate(aspnet_starter_errors_total{environment=\"${environment}\"}[1h])) / clamp_min(sum(rate(aspnet_starter_requests_total{environment=\"${environment}\"}[1h])), 1)"
latency_expression="sum(rate(aspnet_starter_request_duration_milliseconds_total{environment=\"${environment}\"}[1h])) / clamp_min(sum(rate(aspnet_starter_requests_total{environment=\"${environment}\"}[1h])), 1)"

query_range "$error_expression" "$current_start" "$end_time" > "$work_dir/current-errors.json"
query_range "$error_expression" "$previous_start" "$current_start" > "$work_dir/previous-errors.json"
query_range "$latency_expression" "$current_start" "$end_time" > "$work_dir/current-latency.json"
query_range "$latency_expression" "$previous_start" "$current_start" > "$work_dir/previous-latency.json"

current_error="$(summarize "$work_dir/current-errors.json")"
previous_error="$(summarize "$work_dir/previous-errors.json")"
current_latency="$(summarize "$work_dir/current-latency.json")"
previous_latency="$(summarize "$work_dir/previous-latency.json")"
error_change="$(awk -v current="$current_error" -v previous="$previous_error" 'BEGIN { if (previous == 0) print 0; else printf "%.4f", ((current - previous) / previous) * 100 }')"
latency_change="$(awk -v current="$current_latency" -v previous="$previous_latency" 'BEGIN { if (previous == 0) print 0; else printf "%.4f", ((current - previous) / previous) * 100 }')"
status="ok"
if awk -v error="$current_error" -v error_change="$error_change" -v latency_change="$latency_change" 'BEGIN { exit !((error > 0.005) || (error_change > 100) || (latency_change > 100)) }'; then
  status="critical"
elif awk -v error="$current_error" -v error_change="$error_change" -v latency_change="$latency_change" 'BEGIN { exit !((error > 0.001) || (error_change > 20) || (latency_change > 20)) }'; then
  status="review"
fi

generated_at="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
start_date="$(date -u -r "$current_start" +%Y-%m-%d)"
end_date="$(date -u -r "$end_time" +%Y-%m-%d)"
previous_start_date="$(date -u -r "$previous_start" +%Y-%m-%d)"

jq -n \
  --arg generated_at "$generated_at" --arg environment "$environment" \
  --arg status "$status" --arg period_start "$start_date" --arg period_end "$end_date" \
  --arg previous_period_start "$previous_start_date" \
  --argjson period_days "$period_days" --argjson current_error_ratio "$current_error" \
  --argjson previous_error_ratio "$previous_error" --argjson error_change_percent "$error_change" \
  --argjson current_average_latency_ms "$current_latency" --argjson previous_average_latency_ms "$previous_latency" \
  --argjson latency_change_percent "$latency_change" \
  '{generated_at: $generated_at, environment: $environment, status: $status, period_days: $period_days, current_period: {start: $period_start, end: $period_end, error_ratio: $current_error_ratio, average_latency_ms: $current_average_latency_ms}, previous_period: {start: $previous_period_start, end: $period_start, error_ratio: $previous_error_ratio, average_latency_ms: $previous_average_latency_ms}, change_percent: {error_ratio: $error_change_percent, average_latency_ms: $latency_change_percent}, availability_slo: 0.999}' \
  | tee "$json_path"

cat > "$markdown_path" <<EOF
# Reliability trend report

- Environment: `${environment}`
- Status: **${status}**
- Current period: ${start_date} to ${end_date} (${period_days} days)
- Previous period: ${previous_start_date} to ${start_date}
- Generated: ${generated_at}

| Measure | Current | Previous | Change |
| --- | ---: | ---: | ---: |
| Availability error ratio | ${current_error} | ${previous_error} | ${error_change}% |
| Average latency (ms) | ${current_latency} | ${previous_latency} | ${latency_change}% |

The report uses the configured 99.9% availability starting SLO. Review status
requires a quarterly SLO review; critical status requires incident follow-up.
Use traces, deployment annotations, and the incident response runbook to explain
material changes before changing an SLO target.
EOF

echo "Reliability trend status: ${status} (${environment})"
