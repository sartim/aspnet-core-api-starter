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

The release version is supplied by Nerdbank.GitVersioning, so the artifacts
use the same SemVer-derived version as the Git tag and container image.

Swagger is disabled in production by default. Enable it only in a controlled
development or contract-test environment; never expose it publicly without the
adopting team's access policy.
