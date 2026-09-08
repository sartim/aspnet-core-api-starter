#!/usr/bin/env bash
set -euo pipefail

usage() {
  echo "Usage: $0 <base-url> [full|minimal]" >&2
  exit 2
}

base_url=${1:-}
profile=${2:-full}
[[ -n "$base_url" ]] || usage
[[ "$profile" == "full" || "$profile" == "minimal" ]] || {
  echo "Profile must be full or minimal: $profile" >&2
  exit 1
}
base_url="${base_url%/}"

request() {
  local path=$1
  local response_file
  response_file="$(mktemp)"
  trap 'rm -f "$response_file"' RETURN
  curl --fail --silent --show-error --retry 3 --retry-all-errors --retry-delay 2 \
    --connect-timeout 5 --max-time 20 --dump-header "$response_file" \
    "${base_url}${path}" >/dev/null
  grep -qi '^x-trace-id:' "$response_file" || {
    echo "${path} did not return an X-Trace-Id header." >&2
    return 1
  }
}

echo "Checking ${base_url} (${profile} profile)"
request "/health/live"
request "/health/ready"

metrics="$(curl --fail --silent --show-error --retry 3 --retry-all-errors --retry-delay 2 \
  --connect-timeout 5 --max-time 20 "${base_url}/metrics")"
grep -q 'aspnet_starter_requests_total' <<< "$metrics" || {
  echo "/metrics did not expose starter request metrics." >&2
  exit 1
}

if [[ "$profile" == "full" ]]; then
  request "/api/v1/health"
fi

echo "Synthetic endpoint checks passed."
