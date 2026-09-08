#!/usr/bin/env bash
set -euo pipefail

usage() {
  echo "Usage: $0 <prometheus-url> <environment> [report-path]" >&2
  exit 2
}

prometheus_url=${1:-}
environment=${2:-}
report_path=${3:-${SLO_REPORT_PATH:-slo-burn-rate.json}}
[[ -n "$prometheus_url" && -n "$environment" ]] || usage
prometheus_url="${prometheus_url%/}"

query() {
  local expression=$1
  local response
  if [[ -n "${PROMETHEUS_BEARER_TOKEN:-}" ]]; then
    response="$(curl --fail --silent --show-error --retry 3 --retry-all-errors --retry-delay 2 \
      --connect-timeout 5 --max-time 20 \
      -H "Authorization: Bearer ${PROMETHEUS_BEARER_TOKEN}" -G \
      --data-urlencode "query=${expression}" "${prometheus_url}/api/v1/query")"
  else
    response="$(curl --fail --silent --show-error --retry 3 --retry-all-errors --retry-delay 2 \
      --connect-timeout 5 --max-time 20 -G \
      --data-urlencode "query=${expression}" "${prometheus_url}/api/v1/query")"
  fi
  jq -e '.status == "success" and (.data.result | length) > 0' <<< "$response" >/dev/null || {
    echo "Prometheus returned no usable result for ${environment}: ${expression}" >&2
    return 1
  }
  jq -r '.data.result[0].value[1]' <<< "$response"
}

error_ratio() {
  local window=$1
  query "sum(rate(aspnet_starter_errors_total{environment=\"${environment}\"}[${window}])) / clamp_min(sum(rate(aspnet_starter_requests_total{environment=\"${environment}\"}[${window}])), 1)"
}

one_hour_ratio="$(error_ratio 1h)"
six_hour_ratio="$(error_ratio 6h)"
one_hour_burn="$(awk -v ratio="$one_hour_ratio" 'BEGIN { printf "%.4f", ratio / 0.001 }')"
six_hour_burn="$(awk -v ratio="$six_hour_ratio" 'BEGIN { printf "%.4f", ratio / 0.001 }')"
status="ok"
if awk -v fast="$one_hour_burn" -v slow="$six_hour_burn" 'BEGIN { exit !((fast >= 14.4) || (slow >= 6)) }'; then
  status="critical"
elif awk -v fast="$one_hour_burn" -v slow="$six_hour_burn" 'BEGIN { exit !((fast >= 2) || (slow >= 2)) }'; then
  status="warning"
fi

jq -n \
  --arg generated_at "$(date -u +%Y-%m-%dT%H:%M:%SZ)" \
  --arg environment "$environment" \
  --arg status "$status" \
  --argjson one_hour_error_ratio "$one_hour_ratio" \
  --argjson six_hour_error_ratio "$six_hour_ratio" \
  --argjson one_hour_burn_rate "$one_hour_burn" \
  --argjson six_hour_burn_rate "$six_hour_burn" \
  '{generated_at: $generated_at, environment: $environment, availability_slo: 0.999, error_budget: 0.001, status: $status, windows: {one_hour: {error_ratio: $one_hour_error_ratio, burn_rate: $one_hour_burn_rate}, six_hour: {error_ratio: $six_hour_error_ratio, burn_rate: $six_hour_burn_rate}}}' \
  | tee "$report_path"

echo "SLO burn-rate status: ${status} (${environment})"
