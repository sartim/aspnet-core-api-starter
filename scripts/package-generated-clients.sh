#!/usr/bin/env bash
set -euo pipefail

usage() {
  echo "Usage: $0 <openapi-url> <version> <output-directory>" >&2
  exit 2
}

openapi_url=${1:-}
version=${2:-}
output_dir=${3:-}
[[ -n "$openapi_url" && -n "$version" && -n "$output_dir" ]] || usage

generator_version=${OPENAPI_GENERATOR_VERSION:-2.23.3}
work_dir=$(mktemp -d)
trap 'rm -rf "$work_dir"' EXIT

mkdir -p "$output_dir"
npx --yes "@openapitools/openapi-generator-cli@${generator_version}" generate \
  -i "$openapi_url" \
  -g csharp \
  -o "$work_dir/csharp" \
  --additional-properties="packageName=AspNetCoreApiClient,packageVersion=${version}"
npx --yes "@openapitools/openapi-generator-cli@${generator_version}" generate \
  -i "$openapi_url" \
  -g typescript-fetch \
  -o "$work_dir/typescript" \
  --additional-properties="npmName=aspnet-core-api-client,npmVersion=${version},supportsES6=true"

tar -czf "$output_dir/aspnet-core-api-client-csharp-${version}.tar.gz" -C "$work_dir" csharp
tar -czf "$output_dir/aspnet-core-api-client-typescript-${version}.tar.gz" -C "$work_dir" typescript

cat > "$output_dir/aspnet-core-api-client-${version}.json" <<EOF
{
  "version": "${version}",
  "generator": "@openapitools/openapi-generator-cli@${generator_version}",
  "source": "${openapi_url}",
  "artifacts": [
    "aspnet-core-api-client-csharp-${version}.tar.gz",
    "aspnet-core-api-client-typescript-${version}.tar.gz"
  ]
}
EOF

echo "Generated versioned client packages in $output_dir."
