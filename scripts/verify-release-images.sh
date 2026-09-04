#!/usr/bin/env bash
set -euo pipefail

usage() {
  echo "Usage: $0 <promoted-image> <rollback-image>" >&2
  exit 2
}

current_image=${1:-}
rollback_image=${2:-}
[[ -n "$current_image" && -n "$rollback_image" ]] || usage

network="release-verification-${RANDOM}"
postgres="${network}-postgres"
api="${network}-api"
port="${RELEASE_VERIFY_PORT:-18080}"

cleanup() {
  docker rm -f "$api" "$postgres" >/dev/null 2>&1 || true
  docker network rm "$network" >/dev/null 2>&1 || true
}
trap cleanup EXIT

docker network create "$network" >/dev/null
docker run -d --name "$postgres" --network "$network" \
  -e POSTGRES_USER=shopuser -e POSTGRES_PASSWORD=shoppassword \
  -e POSTGRES_DB=shopdb postgres:15 >/dev/null

for attempt in $(seq 1 30); do
  if docker exec "$postgres" pg_isready -U shopuser -d shopdb >/dev/null 2>&1; then
    break
  fi
  [[ "$attempt" -lt 30 ]] || { echo "PostgreSQL did not become ready." >&2; exit 1; }
  sleep 2
done

verify_image() {
  local image=$1
  local label=$2
  echo "Verifying ${label}: ${image}"
  docker pull "$image"
  docker run -d --name "$api" --network "$network" -p "${port}:5070" \
    -e ENV=Production -e PORT=5070 \
    -e DB_URL='Host=postgres;Port=5432;Database=shopdb;Username=shopuser;Password=shoppassword' \
    -e JWT_SECRET_KEY='release-verification-secret-key-at-least-32-bytes' \
    -e JWT_ISSUER=asp-shop-api -e JWT_AUDIENCE=asp-shop-client \
    "$image" >/dev/null
  curl --fail --retry 30 --retry-all-errors --retry-delay 2 \
    "http://localhost:${port}/health/ready"
  curl --fail "http://localhost:${port}/health/live"
  curl --fail "http://localhost:${port}/metrics" >/dev/null
  docker rm -f "$api" >/dev/null
}

verify_image "$current_image" "promoted release"
verify_image "$rollback_image" "rollback release"
echo "Promoted release and rollback image verification passed."
