# API reference

The full `user-service` profile exposes the following versioned endpoints.
Protected endpoints require an administrator JWT. The minimal profile exposes
only the health and metrics endpoints.

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

Use the token for protected requests:

```bash
export TOKEN='eyJ...'
curl --fail "$API_URL/api/v1/users" \
  -H "Authorization: Bearer $TOKEN"
```

## Users

All user endpoints require the `Administrator` role.

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

All role endpoints require the `Administrator` role.

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

All permission endpoints require the `Administrator` role.

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

All role-permission endpoints require the `Administrator` role. `roleId` and
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
