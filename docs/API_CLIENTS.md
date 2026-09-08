# API clients and contract compatibility

The full profile exposes a controlled Swagger document at
`/swagger/v1/swagger.json` when running in Development or when
`SWAGGER_ENABLED=true` is explicitly configured. The checked-in
`docs/openapi-contract.json` is a route/method compatibility snapshot. CI
starts the Compose API and compares its generated Swagger routes with that
snapshot, so an endpoint addition, removal, or method change is reviewed as a
contract change.

Generate a typed client from the running API with OpenAPI Generator:

```bash
npx @openapitools/openapi-generator-cli generate \
  -i "$API_URL/swagger/v1/swagger.json" \
  -g csharp \
  -o ./generated/ShopApiClient \
  --additional-properties=packageName=ShopApiClient
```

For TypeScript:

```bash
npx @openapitools/openapi-generator-cli generate \
  -i "$API_URL/swagger/v1/swagger.json" \
  -g typescript-fetch \
  -o ./generated/shop-api-client
```

The repository includes minimal [C#](https://github.com/sartim/aspnet-core-api-starter/blob/main/examples/clients/csharp/Program.cs) and
[TypeScript](https://github.com/sartim/aspnet-core-api-starter/blob/main/examples/clients/typescript/client.ts) examples showing the
login, bearer-token, and paginated-users flow. They are examples rather than
runtime dependencies of generated projects.

## Compatibility and release artifacts

Pull requests compare the generated OpenAPI document with
`docs/openapi-compatibility-baseline.json`. Existing routes, methods, response
status codes, parameters, request content types, component schemas, required
properties, and enum values cannot be removed or narrowed without an explicit
baseline update and review. New endpoints and optional response fields are
backward-compatible additions.

Tagged releases attach versioned client artifacts:

- `openapi-contract-VERSION.json` — the route/method contract snapshot.
- `openapi-compatibility-baseline-VERSION.json` — the reviewed compatibility
  baseline.
- `api-client-examples-VERSION.tar.gz` — the C# and TypeScript examples.
- `aspnet-core-api-client-csharp-VERSION.tar.gz` — the generated C# client
  package.
- `aspnet-core-api-client-typescript-VERSION.tar.gz` — the generated TypeScript
  client package.
- `aspnet-core-api-client-VERSION.json` — generator and artifact metadata.
- `aspnet-core-api-client-VERSION.sha256` — checksums for the generated client
  archives.
- `aspnet-core-api-client-VERSION.provenance.json` — SLSA-style build metadata
  linking the packages to the source commit and generator version.

The release version is supplied by Nerdbank.GitVersioning, so the artifacts
use the same SemVer-derived version as the Git tag and container image.
Generated packages are built from the live Swagger document after the release
runtime smoke test. See the [compatibility exception process](COMPATIBILITY_EXCEPTIONS.md)
before intentionally changing the baseline.

## SDK smoke tests and registry publication

Pull requests and pushes to `main` run the **Client SDK smoke tests** workflow.
It generates both clients from the live Swagger document, builds the generated
C# project, installs the generated TypeScript dependencies, and runs its build.
This catches generator-breaking contract changes before a release is tagged.

The default distribution mechanism is the versioned GitHub Release assets. A
team that operates a package registry can publish from the generated outputs:

```bash
# C# / NuGet-compatible registry, after unpacking the C# client archive
dotnet pack path/to/AspNetCoreApiClient.csproj --configuration Release \
  --output ./packages
dotnet nuget push ./packages/*.nupkg \
  --source "$NUGET_SOURCE" --api-key "$NUGET_API_KEY"

# TypeScript / npm-compatible registry, after unpacking the TypeScript archive
npm publish ./path/to/typescript-client --access restricted \
  --registry "$NPM_REGISTRY"
```

Keep registry credentials in the consuming repository or protected deployment
environment. Do not add credentials to this starter or make registry
publication a prerequisite for the GitHub Release; adopters may use NuGet,
npm, GitHub Packages, or an internal registry with the same SemVer version.

The release workflow also emits GitHub artifact attestations for the generated
archives. Consumers can verify the checksums with `sha256sum --check` and use
GitHub's attestation verification for the release repository before unpacking
an SDK.

Swagger is disabled in production by default. Enable it only in a controlled
development or contract-test environment; never expose it publicly without the
adopting team's access policy.
