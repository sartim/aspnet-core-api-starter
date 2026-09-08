# API clients and contract compatibility

The full profile exposes a development-only Swagger document at
`/swagger/v1/swagger.json`. The checked-in
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

Swagger is disabled in production by default. Export the document only in a
controlled development or contract-test environment; never expose it publicly
without the adopting team's access policy.
