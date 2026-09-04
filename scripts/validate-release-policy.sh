#!/usr/bin/env bash
set -euo pipefail

usage() {
  echo "Usage: $0 <staging|production> <version> [policy-file]" >&2
  exit 2
}

environment=${1:-}
version=${2:-}
policy_file=${3:-.github/release-policy.yml}
[[ -n "$environment" && -n "$version" ]] || usage
[[ "$environment" == "staging" || "$environment" == "production" ]] || {
  echo "Unsupported environment: $environment" >&2
  exit 1
}
[[ -f "$policy_file" ]] || { echo "Policy file not found: $policy_file" >&2; exit 1; }

version=${version#v}
[[ "$version" =~ ^[0-9]+\.[0-9]+\.[0-9]+([+-][0-9A-Za-z.-]+)?$ ]] || {
  echo "Version must be SemVer (for example 1.2.3 or 1.2.3-rc.1): $version" >&2
  exit 1
}

policy_section="${TMPDIR:-/tmp}/release-policy.$$"
trap 'rm -f "$policy_section"' EXIT
awk -v section="  $environment:" '
  $0 == section { in_section=1; next }
  in_section && /^  [a-zA-Z0-9_-]+:/ { in_section=0 }
  in_section { print }
' "$policy_file" > "$policy_section"

grep -q '^    required_checks: ci,security,container$' "$policy_section" || {
  echo "Environment $environment must require CI, security, and container checks." >&2
  exit 1
}
grep -q '^    image_tag_suffix: ' "$policy_section" || {
  echo "Environment $environment must define an image tag suffix." >&2
  exit 1
}

if [[ "$environment" == "production" && "$version" == *-* ]]; then
  echo "Production promotion does not allow prerelease versions: $version" >&2
  exit 1
fi

echo "Release policy accepted: environment=$environment version=$version"
