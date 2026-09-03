# Template versioning

Every generated project contains `.aspnet-starter.json` with its profile,
template version, and source commit. The current template line is `1.0.0`.
Inspect it from a generated project with:

```bash
./scripts/aspnet-starter check .
```

Patch releases are expected to be source-compatible. A minor release may add
opt-in capabilities or files. A major release may change generated contracts
and requires a reviewed migration.

## Migration path

1. Run `aspnet-starter check` and record the generated profile and template
   version.
2. Read the release notes for the target template version and apply its
   migration notes to the generated application.
3. Regenerate a clean copy with the target profile, then port application-owned
   code and configuration in a reviewed branch.
4. For database-backed applications, apply EF migrations separately and verify
   rollback before deployment.
5. Commit the updated `.aspnet-starter.json` and run the generated project's
   tests and security checks.

The starter does not overwrite an existing application automatically. This
keeps local customizations, secrets, and provider-specific integrations safe.
