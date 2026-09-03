# Optional distributed caching

The full `user-service` profile registers `IDistributedCache`. It uses the
in-process distributed-memory provider by default, so the API works without
additional infrastructure. Set `REDIS_CONNECTION` to use Redis instead:

```env
REDIS_CONNECTION=redis:6379
```

The minimal profile does not include Redis, caching, EF Core, or user-service
dependencies.

## What is cached

Paginated collection responses from the shared CRUD controller are cached for
30 seconds. The cache key includes the entity type, cache version, page,
page-size, text query, and soft-delete flag. A successful create, update, or
delete advances the entity cache version, invalidating prior collection
responses without requiring key enumeration.

Cache failures are intentionally fail-open: reads continue against PostgreSQL
and writes succeed even if Redis is unavailable. Redis is an optimization, not
the source of truth. Do not cache responses containing user-specific or
authorization-sensitive data in a shared key without adding an identity-aware
key or moving that endpoint out of the generic controller.

## Compose

Run the optional overlay when local distributed-cache behavior is needed:

```bash
docker compose -f docker-compose.yaml -f docker-compose.redis.yaml up -d --build
```

The base Compose stack remains the supported default and does not start Redis.
For production, provide Redis through the platform's managed service and keep
the connection string in the secret manager. Configure persistence, TLS,
authentication, eviction, and availability according to the chosen Redis
provider.
