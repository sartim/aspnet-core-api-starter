#!/usr/bin/env bash
set -euo pipefail

usage() {
  echo "Usage: $0 <artifact-directory> <version> <commit> <generator-version> <source-url>" >&2
  exit 2
}

artifact_dir=${1:-}
version=${2:-}
commit=${3:-}
generator_version=${4:-}
source_url=${5:-}
[[ -d "$artifact_dir" && -n "$version" && -n "$commit" && -n "$generator_version" && -n "$source_url" ]] || usage

checksum_file="$artifact_dir/aspnet-core-api-client-${version}.sha256"
provenance_file="$artifact_dir/aspnet-core-api-client-${version}.provenance.json"
sha256sum "$artifact_dir"/aspnet-core-api-client-*-${version}.tar.gz > "$checksum_file"

artifacts=$(jq -Rsc '
  split("\n")
  | map(select(length > 0) | capture("^(?<sha>[0-9a-f]+)  (?<path>.*)$")
      | {name: (.path | split("/") | last), sha256: .sha})
' "$checksum_file")

jq -n \
  --arg version "$version" \
  --arg commit "$commit" \
  --arg generator "@openapitools/openapi-generator-cli@$generator_version" \
  --arg source "$source_url" \
  --arg repository "${GITHUB_REPOSITORY:-}" \
  --arg workflow "${GITHUB_WORKFLOW:-}" \
  --arg run_id "${GITHUB_RUN_ID:-}" \
  --argjson artifacts "$artifacts" \
  '{
    _type: "https://in-toto.io/Statement/v1",
    predicateType: "https://slsa.dev/provenance/v1",
    subject: $artifacts,
    predicate: {
      buildDefinition: {
        buildType: "https://github.com/actions/runner",
        externalParameters: {
          repository: $repository,
          workflow: $workflow,
          runId: $run_id,
          sourceOpenApi: $source,
          generator: $generator,
          version: $version
        },
        resolvedDependencies: [{uri: $source, digest: {gitCommit: $commit}}]
      },
      runDetails: {
        builder: {id: "https://github.com/actions/runner"},
        metadata: {invocationId: $run_id}
      }
    }
  }' > "$provenance_file"

echo "Created $checksum_file and $provenance_file."
