#!/usr/bin/env bash
set -euo pipefail

usage() {
  echo "Usage: $0 <openapi-url>" >&2
  exit 2
}

openapi_url=${1:-}
[[ -n "$openapi_url" ]] || usage

generator_version=${OPENAPI_GENERATOR_VERSION:-2.23.3}
work_dir=$(mktemp -d)
trap 'rm -rf "$work_dir"' EXIT

generate() {
  local generator=$1
  local output=$2
  shift 2
  npx --yes "@openapitools/openapi-generator-cli@${generator_version}" generate \
    -i "$openapi_url" \
    -g "$generator" \
    -o "$output" \
    "$@"
}

generate csharp "$work_dir/csharp" \
  --additional-properties="packageName=AspNetCoreApiClient,packageVersion=0.0.0-smoke"
csharp_project=$(find "$work_dir/csharp" -name '*.csproj' -type f -print -quit)
[[ -n "$csharp_project" ]] || { echo "Generated C# project not found." >&2; exit 1; }
dotnet build "$csharp_project" --configuration Release --nologo

generate typescript-fetch "$work_dir/typescript" \
  --additional-properties="npmName=aspnet-core-api-client,npmVersion=0.0.0-smoke,supportsES6=true"
[[ -f "$work_dir/typescript/package.json" ]] || { echo "Generated TypeScript package not found." >&2; exit 1; }
npm --prefix "$work_dir/typescript" install --ignore-scripts --no-audit --no-fund
npm --prefix "$work_dir/typescript" run build

echo "Generated C# and TypeScript clients build successfully."
