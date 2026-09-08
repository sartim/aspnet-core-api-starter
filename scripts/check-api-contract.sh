#!/usr/bin/env bash
set -euo pipefail

usage() {
  echo "Usage: $0 <openapi-url> <snapshot-json>" >&2
  exit 2
}

openapi_url=${1:-}
snapshot=${2:-}
[[ -n "$openapi_url" && -n "$snapshot" ]] || usage
[[ -f "$snapshot" ]] || { echo "Snapshot not found: $snapshot" >&2; exit 1; }

actual="$(curl --fail --silent --show-error --retry 5 --retry-all-errors --retry-delay 2 \
  --connect-timeout 5 --max-time 30 "$openapi_url" \
  | jq -c '[.paths | to_entries[] as $entry | $entry.value | keys[] as $method | {method: ($method | ascii_upcase), path: ($entry.key | ascii_downcase)}] | sort_by(.path, .method)')"
expected="$(jq -c 'sort_by(.path, .method)' "$snapshot")"

if [[ "$actual" != "$expected" ]]; then
  echo "OpenAPI route contract changed." >&2
  diff -u <(jq -r '.[] | "\(.method) \(.path)"' <<< "$expected") \
    <(jq -r '.[] | "\(.method) \(.path)"' <<< "$actual") || true
  exit 1
fi

echo "OpenAPI route contract matches $snapshot."
