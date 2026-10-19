#!/usr/bin/env bash
set -euo pipefail

usage() {
  echo "Usage: $0 <owner/repository> <staging|production>" >&2
  exit 2
}

repository=${1:-}
environment=${2:-}
[[ -n "$repository" && -n "$environment" ]] || usage
[[ "$environment" == "staging" || "$environment" == "production" ]] || {
  echo "Unsupported environment: $environment" >&2
  exit 1
}

image="ghcr.io/${repository}"
deployment_id=""
ref=""
description=""
while IFS= read -r candidate; do
  [[ -n "$candidate" ]] || continue
  state="$(gh api "repos/${repository}/deployments/${candidate}/statuses?per_page=1" --jq '.[0].state // empty')"
  [[ "$state" == "success" ]] || continue

  candidate_ref="$(gh api "repos/${repository}/deployments/${candidate}" --jq '.ref')"
  candidate_version="${candidate_ref#v}"
  [[ "$candidate_version" =~ ^[0-9]+\.[0-9]+\.[0-9]+([+-][0-9A-Za-z.-]+)?$ ]] || continue

  deployment_id="$candidate"
  ref="$candidate_ref"
  description="$(gh api "repos/${repository}/deployments/${deployment_id}" --jq '.description // ""')"
  break
done < <(gh api "repos/${repository}/deployments?environment=${environment}&per_page=20" --jq '.[].id')

[[ -n "$deployment_id" ]] || {
  message="No successful versioned deployment record found for ${environment}; drift verification skipped."
  echo "$message"
  if [[ -n "${GITHUB_STEP_SUMMARY:-}" ]]; then
    printf '%s\n' "### Deployment drift check skipped" "" "$message" >> "$GITHUB_STEP_SUMMARY"
  fi
  exit 0
}

version="${ref#v}"
digest="$(printf '%s\n' "$description" | sed -nE 's/.*\((sha256:[0-9a-f]{64})\).*/\1/p')"
rollback_version="$(printf '%s\n' "$description" | sed -nE 's/.*rollback=([0-9]+\.[0-9]+\.[0-9]+([+-][0-9A-Za-z.-]+)?).*/\1/p')"

[[ "$version" =~ ^[0-9]+\.[0-9]+\.[0-9]+([+-][0-9A-Za-z.-]+)?$ ]] || {
  echo "Deployment ${deployment_id} has an invalid release ref: ${ref}" >&2
  exit 1
}
[[ "$digest" =~ ^sha256:[0-9a-f]{64}$ ]] || {
  echo "Deployment ${deployment_id} has no valid image digest in its description." >&2
  exit 1
}
[[ "$rollback_version" =~ ^[0-9]+\.[0-9]+\.[0-9]+([+-][0-9A-Za-z.-]+)?$ ]] || {
  echo "Deployment ${deployment_id} has no valid rollback version." >&2
  exit 1
}

current_image="${image}:${version}-${environment}"
rollback_image="${image}:${rollback_version}-${environment}"
docker pull "$current_image" >/dev/null
actual_digest="$(docker image inspect "$current_image" --format '{{index .RepoDigests 0}}')"
actual_digest="${actual_digest##*@}"
[[ "$actual_digest" == "$digest" ]] || {
  echo "Deployment drift detected: expected ${digest}, registry has ${actual_digest}." >&2
  exit 1
}
docker pull "$rollback_image" >/dev/null
gh attestation verify "oci://${image}@${digest}" --repo "$repository" >/dev/null

echo "Deployment healthy: environment=${environment} version=${version} digest=${digest} rollback=${rollback_version}"
