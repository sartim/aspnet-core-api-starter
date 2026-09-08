# API compatibility exceptions

The default policy is to keep existing API consumers working. The hosted
OpenAPI compatibility check therefore blocks removals and narrowing changes to
routes, methods, parameters, request content, responses, schemas, required
properties, and enum values.

## When an exception is justified

An exception is appropriate only when retaining the old contract is unsafe,
incorrect, or materially blocks a planned major-version change. A convenience
refactor or an unreviewed generated-client update is not an exception.

## Required PR process

1. Leave the compatibility check enabled and describe the failed contract
   entries in the PR.
2. Add the affected consumers, migration owner, rollout plan, and rollback
   plan to the PR description. Link the tracking issue for the exception.
3. State the SemVer impact: breaking changes require a major version; a
   deprecation period should be used when consumers can migrate gradually.
4. Add replacement endpoints or fields before removing the old contract when
   an expand-and-contract migration is possible.
5. Update `docs/openapi-compatibility-baseline.json` in the same approved PR,
   with a short comment in the PR explaining why the removed guarantee is
   intentional.
6. Regenerate and publish the versioned C# and TypeScript client packages.
   Include migration notes in the GitHub Release.

The compatibility baseline is never changed solely to make CI green. Reviewers
should reject a baseline update that does not include consumer impact and a
versioning decision.
