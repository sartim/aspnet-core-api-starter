#!/usr/bin/env bash
set -euo pipefail

usage() {
  echo "Usage: CAPACITY_FIXTURE_CONFIRM=I_UNDERSTAND_NON_PRODUCTION DATABASE_URL=... $0 [users] [roles] [permissions]" >&2
  exit 2
}

[[ "${CAPACITY_FIXTURE_CONFIRM:-}" == "I_UNDERSTAND_NON_PRODUCTION" ]] || {
  echo "Refusing to seed data without CAPACITY_FIXTURE_CONFIRM=I_UNDERSTAND_NON_PRODUCTION." >&2
  exit 1
}
database_url=${DATABASE_URL:-${DB_URL:-}}
users=${1:-10000}
roles=${2:-100}
permissions=${3:-250}
[[ -n "$database_url" ]] || usage
[[ "$users" =~ ^[0-9]+$ && "$roles" =~ ^[0-9]+$ && "$permissions" =~ ^[0-9]+$ ]] || usage
[[ "$users" -gt 0 && "$roles" -gt 0 && "$permissions" -gt 0 ]] || usage

psql "$database_url" -v ON_ERROR_STOP=1 \
  -v users="$users" -v roles="$roles" -v permissions="$permissions" \
  -f "$(dirname "$0")/seed-capacity-fixture.sql"

psql "$database_url" -v ON_ERROR_STOP=1 -c \
  'SELECT count(*) AS users FROM "Users" WHERE "Email" LIKE '\''capacity.user.%@example.test'\'';'
psql "$database_url" -v ON_ERROR_STOP=1 -c \
  'SELECT count(*) AS roles FROM "Roles" WHERE "Name" LIKE '\''capacity-role-%'\'';'
psql "$database_url" -v ON_ERROR_STOP=1 -c \
  'SELECT count(*) AS permissions FROM "Permissions" WHERE "Name" LIKE '\''capacity.permission.%'\'';'
