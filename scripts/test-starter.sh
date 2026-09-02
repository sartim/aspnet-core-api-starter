#!/usr/bin/env bash
set -euo pipefail

source_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
test_root="$(mktemp -d "${TMPDIR:-/tmp}/aspnet-starter-smoke.XXXXXX")"
trap 'rm -rf "$test_root"' EXIT

minimal="$test_root/minimal"
full="$test_root/full"

"$source_root/scripts/aspnet-starter" init catalog-api "$minimal" --profile minimal
"$source_root/scripts/aspnet-starter" init accounts-api "$full"

test -f "$minimal/catalog-api.csproj"
test -f "$full/AspNetCoreApiStarter/AspNetCoreApiStarter.csproj"

if rg -n 'User|Role|Permission|EntityFramework|JWT|Sentry' "$minimal" --glob '*.cs' --glob '*.csproj' --glob '*.json'; then
  echo "Minimal profile contains user-service or vendor-specific source." >&2
  exit 1
fi

rg -q 'UserController|RoleController|PermissionController' "$full/AspNetCoreApiStarter" --glob '*.cs'

dotnet restore "$minimal/catalog-api.csproj"
dotnet build "$minimal/catalog-api.csproj" --configuration Release --no-restore
dotnet restore "$full/AspNetCoreApiStarter/AspNetCoreApiStarter.csproj"
dotnet build "$full/AspNetCoreApiStarter/AspNetCoreApiStarter.csproj" --configuration Release --no-restore

echo "Starter profile smoke tests passed."
