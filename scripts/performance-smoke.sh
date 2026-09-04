#!/usr/bin/env bash
set -euo pipefail

url=${1:-http://localhost:5070/health/live}
requests=${2:-40}
max_p95_ms=${3:-1000}
results_file="$(mktemp)"
trap 'rm -f "$results_file"' EXIT

for _ in $(seq 1 "$requests"); do
  curl --silent --show-error --max-time 10 --output /dev/null \
    --write-out '%{http_code} %{time_total}\n' "$url" >> "$results_file"
done

successes=$(awk '$1 ~ /^2/ { count++ } END { print count + 0 }' "$results_file")
if [[ "$successes" -ne "$requests" ]]; then
  echo "Performance smoke failed: $((requests - successes))/$requests requests were not successful." >&2
  cat "$results_file" >&2
  exit 1
fi

p95_index=$(( (requests * 95 + 99) / 100 ))
p95_ms=$(awk '{ print $2 * 1000 }' "$results_file" | sort -n | sed -n "${p95_index}p" | awk '{ printf "%.0f", $1 }')
echo "Performance smoke: requests=$requests p95_ms=$p95_ms threshold_ms=$max_p95_ms"
if [[ "$p95_ms" -gt "$max_p95_ms" ]]; then
  echo "Performance smoke failed: p95 latency exceeded the threshold." >&2
  exit 1
fi
