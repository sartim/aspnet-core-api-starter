#!/usr/bin/env bash
set -euo pipefail

usage() {
  echo "Usage: $0 <openapi-url>" >&2
  exit 2
}

openapi_url=${1:-}
[[ -n "$openapi_url" ]] || usage

package_dir=$(mktemp -d)
consumer_dir=$(mktemp -d)
trap 'rm -rf "$package_dir" "$consumer_dir"' EXIT

./scripts/package-generated-clients.sh "$openapi_url" "0.0.0-smoke" "$package_dir"

mkdir -p "$consumer_dir/csharp" "$consumer_dir/typescript"
tar -xzf "$package_dir/aspnet-core-api-client-csharp-0.0.0-smoke.tar.gz" -C "$consumer_dir/csharp"
tar -xzf "$package_dir/aspnet-core-api-client-typescript-0.0.0-smoke.tar.gz" -C "$consumer_dir/typescript"

csharp_project=$(find "$consumer_dir/csharp" -name '*.csproj' -type f -print -quit)
[[ -n "$csharp_project" ]] || { echo "Archived C# client project not found." >&2; exit 1; }
dotnet build "$csharp_project" --configuration Release --nologo

[[ -f "$consumer_dir/typescript/typescript/package.json" ]] || {
  echo "Archived TypeScript client package not found." >&2
  exit 1
}
npm --prefix "$consumer_dir/typescript/typescript" install --ignore-scripts --no-audit --no-fund
npm --prefix "$consumer_dir/typescript/typescript" run build

echo "Archived C# and TypeScript client packages build successfully as consumers."
