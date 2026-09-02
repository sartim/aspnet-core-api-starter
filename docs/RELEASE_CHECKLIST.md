# Release checklist

Use this checklist for every production release. The release workflow creates a
`vX.Y.Z` tag only after CI passes; deployment remains a deliberate operator
action.

## Prepare

- [ ] Confirm the change is on the intended release branch and the working tree
      is clean.
- [ ] Select the semantic version: increment major for breaking API changes,
      minor for backward-compatible features, and patch for fixes.
- [ ] Review the changelog, migration notes, and API compatibility impact.
- [ ] Confirm the generated `user-service` and `minimal` profiles still match
      their documented dependency boundaries.

## Database and migrations

- [ ] Review every new EF Core migration for destructive operations and data
      loss.
- [ ] Prefer expand-and-contract changes: add compatible schema first, migrate
      data, then remove obsolete schema in a later release.
- [ ] Back up production data and verify the restore procedure before applying
      a risky migration.
- [ ] Apply migrations during a controlled deployment window and verify health
      plus the expected Users/RBAC schema afterward.
- [ ] Record the migration state and a rollback plan. Do not blindly roll back
      a migration that changed or deleted production data.

## Secrets and configuration

- [ ] Confirm production values exist for `PORT`, `DB_URL`,
      `JWT_SECRET_KEY`, `JWT_ISSUER`, and `JWT_AUDIENCE`.
- [ ] Keep secrets in the deployment secret manager; never put them in Git,
      images, release notes, or logs.
- [ ] Verify JWT key rotation, token expiry, Sentry DSN, and OTLP endpoint
      behavior for the target environment.
- [ ] Confirm telemetry excludes passwords, tokens, credentials, and
      unnecessary personal data.

## Image and release artifacts

- [ ] Build the Docker image from the reviewed commit and tag it with the
      immutable Git SHA and release version.
- [ ] Confirm the `production` GitHub Environment has required reviewers before
      the image-publishing job is allowed to run.
- [ ] Scan the image and dependencies for vulnerabilities.
- [ ] Record the published image digest and verify the SBOM/provenance
      attestations.
- [ ] Verify the image starts with the production configuration and passes the
      health endpoint check.
- [ ] Confirm the GitHub Release contains the correct `vX.Y.Z` tag and notes.
- [ ] Confirm the image metadata artifact is attached to the GitHub Release.

## Compatibility and deployment

- [ ] Verify existing clients can continue using unchanged endpoints and
      response fields.
- [ ] Document any new required configuration, response changes, or migration
      sequencing before deployment.
- [ ] Deploy through the protected environment and monitor error rate,
      latency, logs, traces, and metrics.
- [ ] Confirm `/api/v1/health` and `/metrics` after deployment.

## Rollback and closeout

- [ ] Define the rollback image and application version before deployment.
- [ ] If a migration is backward-compatible, roll back the application image
      first; use a tested database recovery plan for destructive changes.
- [ ] Record the deployed image digest, release tag, migration level, and
      operator decision.
- [ ] Close follow-up issues for deferred cleanup, deprecations, and alerts.
