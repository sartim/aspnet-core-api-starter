---
layout: default
title: ASP.NET Core API Starter
---

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
- [Roadmap and priorities](ROADMAP.html)
- [Source repository](https://github.com/sartim/aspnet-core-api-starter)

The documentation site is built from Markdown by GitHub Actions and published
to GitHub Pages.
