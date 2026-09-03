# API reference

The full `user-service` profile exposes the following versioned endpoints.
Protected endpoints require a JWT with the relevant role or permission. The
`Administrator` role is a superuser for the included management policies. The
minimal profile exposes only the health and metrics endpoints.

Set a shell variable for the examples:

```bash
export API_URL=http://localhost:5070
```

## Health

`GET /api/v1/health` is anonymous and checks database connectivity in the full
profile.

```bash
curl --fail "$API_URL/api/v1/health"
```

Successful full-profile response:

```json
{"status":"Healthy","db":"OK","timestamp":"2026-01-01T00:00:00Z"}
```

The minimal profile returns the same status and timestamp fields without the
`db` field.

## Authentication

`POST /api/v1/auth/generate-jwt` is anonymous. Create an administrator first
with `dotnet run -- --create-admin`, then exchange the credentials for a JWT:

```bash
curl --fail "$API_URL/api/v1/auth/generate-jwt" \
  -H 'Content-Type: application/json' \
  -d '{"email":"admin@example.com","password":"change-this-password"}'
```

Successful response:

```json
{
  "token": "eyJ...",
  "user": {
    "firstName": "Administrator",
    "email": "admin@example.com",
    "roles": ["Administrator"]
  }
}
```

Invalid credentials return `401 Unauthorized`.

Access tokens expire after `JWT_EXPIRY` seconds (300 by default). Login also
returns a one-time refresh token valid for `JWT_REFRESH_EXPIRY` seconds (7 days
by default). Refresh tokens are stored as hashes and rotated on every refresh;
reusing a rotated or revoked token returns `401 Unauthorized`.

```bash
curl --fail "$API_URL/api/v1/auth/refresh" \
  -H 'Content-Type: application/json' \
  -d '{"refreshToken":"REFRESH_TOKEN"}'
```

Revoke the current access token and all refresh tokens for its account:

```bash
curl --fail -X POST "$API_URL/api/v1/auth/revoke" \
  -H "Authorization: Bearer $TOKEN"
```

Passwords must be at least 12 characters and contain uppercase, lowercase,
numeric, and special characters. After `AUTH_MAX_FAILED_ATTEMPTS` failed
logins (5 by default), the account is locked for `AUTH_LOCKOUT_MINUTES` (15 by
default). Failed-login responses intentionally do not reveal whether an email
exists or whether an account is locked.

Use the token for protected requests. Collection endpoints return a stable
response envelope and support `page` (default `1`), `pageSize` (default `25`,
maximum `100`), `q` for case-insensitive text search, and `includeDeleted` for
soft-deletable resources:

```bash
export TOKEN='eyJ...'
curl --fail "$API_URL/api/v1/users" \
  -H "Authorization: Bearer $TOKEN"
```

Example response:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 25,
  "totalCount": 0,
  "totalPages": 0
}
```

Invalid paging values return `400 Bad Request` as `application/problem+json`.
Single-resource lookups remain available with `?id=<resource-id>` and return
the resource directly; update and delete routes use `/api/v1/<resource>/{id}`.

## Authorization policies

The full profile issues permission claims from a user's assigned roles and
evaluates these policies at the controller boundary:

| Policy | Required permission | Protected resource |
| --- | --- | --- |
| `users.manage` | `users.manage` | `/api/v1/users` |
| `roles.manage` | `roles.manage` | `/api/v1/roles` |
| `permissions.manage` | `permissions.manage` | `/api/v1/permissions` |
| `role-permissions.manage` | `role-permissions.manage` | `/api/v1/role-permissions` |

Create the permission records and role-permission links through the included
RBAC endpoints, then issue a new JWT for the changes to take effect. Existing
tokens do not change until they expire or are reissued.

## Users

All user endpoints require the `users.manage` policy.

| Method | Endpoint | Purpose |
| --- | --- | --- |
| `GET` | `/api/v1/users` | List users |
| `POST` | `/api/v1/users` | Create a user; the password is hashed before storage |
| `PUT` | `/api/v1/users/{id}` | Update a user |
| `DELETE` | `/api/v1/users/{id}` | Delete a user |

Example create request:

```bash
curl --fail "$API_URL/api/v1/users" \
  -H "Authorization: Bearer $TOKEN" \
  -H 'Content-Type: application/json' \
  -d '{
    "firstName":"Jane",
    "lastName":"Doe",
    "email":"jane@example.com",
    "phone":254700000000,
    "password":"use-a-secret-manager"
  }'
```

## Roles

All role endpoints require the `roles.manage` policy.

| Method | Endpoint | Purpose |
| --- | --- | --- |
| `GET` | `/api/v1/roles` | List roles |
| `POST` | `/api/v1/roles` | Create a role |
| `PUT` | `/api/v1/roles/{id}` | Update a role |
| `DELETE` | `/api/v1/roles/{id}` | Delete a role |

```bash
curl --fail "$API_URL/api/v1/roles" \
  -H "Authorization: Bearer $TOKEN" \
  -H 'Content-Type: application/json' \
  -d '{"name":"Support","description":"Support team access"}'
```

## Permissions

All permission endpoints require the `permissions.manage` policy.

| Method | Endpoint | Purpose |
| --- | --- | --- |
| `GET` | `/api/v1/permissions` | List permissions |
| `POST` | `/api/v1/permissions` | Create a permission |
| `PUT` | `/api/v1/permissions/{id}` | Update a permission |
| `DELETE` | `/api/v1/permissions/{id}` | Delete a permission |

```bash
curl --fail "$API_URL/api/v1/permissions" \
  -H "Authorization: Bearer $TOKEN" \
  -H 'Content-Type: application/json' \
  -d '{"name":"users.read","description":"View users"}'
```

## Role permissions

All role-permission endpoints require the `role-permissions.manage` policy. `roleId` and
`permissionId` are GUIDs returned by the roles and permissions endpoints.

| Method | Endpoint | Purpose |
| --- | --- | --- |
| `GET` | `/api/v1/role-permissions` | List role-permission links |
| `POST` | `/api/v1/role-permissions` | Grant a permission to a role |
| `PUT` | `/api/v1/role-permissions/{id}` | Update a link |
| `DELETE` | `/api/v1/role-permissions/{id}` | Revoke a link |

```bash
curl --fail "$API_URL/api/v1/role-permissions" \
  -H "Authorization: Bearer $TOKEN" \
  -H 'Content-Type: application/json' \
  -d '{"roleId":"ROLE_GUID","permissionId":"PERMISSION_GUID"}'
```

## Metrics and tracing

`GET /metrics` is anonymous and returns Prometheus-compatible metrics. API
responses include an `X-Trace-Id` header for correlation with structured logs
and distributed traces.
