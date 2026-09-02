# ASP.NET Core API Starter

Production-oriented ASP.NET Core API starter with two generated profiles:

- **user-service** — Users, RBAC, JWT authentication, PostgreSQL, migrations,
  Docker, and tests.
- **minimal** — dependency-free ASP.NET Core API foundation with health,
  logging, tracing, metrics, and no user-service dependencies.

## Generate a project

```bash
./scripts/aspnet-starter init shop-api ../shop-api
./scripts/aspnet-starter init shop-api ../shop-api --profile minimal
```

## Documentation

- [Project README](https://github.com/sartim/aspnet-core-api-starter#readme)
- [Roadmap and priorities](ROADMAP.md)
- [Source repository](https://github.com/sartim/aspnet-core-api-starter)

!!! tip "Choose a profile"

    Use the default `user-service` profile when you need Users, RBAC, JWT
    authentication, and PostgreSQL. Use `--profile minimal` for a clean API
    foundation without the user-service dependencies.

!!! info "Documentation publishing"

    This site is generated from Markdown with MkDocs Material by GitHub Actions
    and published to GitHub Pages. The local repository does not need Python or
    MkDocs installed.
