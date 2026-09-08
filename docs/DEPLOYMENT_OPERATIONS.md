# Deployment operations

The **Deployment drift monitor** workflow runs daily and can also be started
manually for staging, production, or both environments. It uses the latest
successful GitHub Deployment record to verify that:

- the recorded image digest still matches the environment-tagged image in GHCR;
- the recorded rollback image is still available; and
- the deployed image still has a valid GitHub artifact attestation.

If a check fails, the workflow opens one deduplicated `[deployment-drift]`
issue per environment. Close the issue after correcting the deployment or
recording a new verified deployment. The workflow requires `issues: write` and
package read access but does not require production environment approval because
it is read-only verification.
