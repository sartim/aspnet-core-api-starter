-- Non-production fixture for capacity testing. Execute through
-- scripts/seed-capacity-fixture.sh so counts are explicit and guarded.

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

WITH fixture_users AS (
  SELECT gs, uuid_generate_v5(uuid_ns_url(), 'aspnet-starter-capacity-user-' || gs::text) AS id
  FROM generate_series(1, :'users'::int) AS gs
)
INSERT INTO "Users" (
  "Id", "FirstName", "LastName", "Email", "Phone", "Password", "IsActive",
  "IsDeleted", "CreatedAt", "UpdatedAt", "DeletedAt", "FailedLoginAttempts",
  "LockoutEnd", "LastLoginAt", "EmailVerifiedAt"
)
SELECT id, 'Capacity', 'User ' || gs, 'capacity.user.' || gs || '@example.test',
  100000000 + gs, '$2a$11$7QJ8mYw2rKJvZQxY3vQpOe6Qv8wT5T7p3QeJzVxR1kL9fH2mN4s6u',
  true, false, now(), now(), null, 0, null, null, now()
FROM fixture_users
ON CONFLICT ("Id") DO NOTHING;

WITH fixture_roles AS (
  SELECT gs, uuid_generate_v5(uuid_ns_url(), 'aspnet-starter-capacity-role-' || gs::text) AS id
  FROM generate_series(1, :'roles'::int) AS gs
)
INSERT INTO "Roles" ("Id", "Name", "Description", "IsDeleted", "CreatedAt", "UpdatedAt", "DeletedAt")
SELECT id, 'capacity-role-' || gs, 'Non-production capacity fixture role ' || gs,
  false, now(), now(), null
FROM fixture_roles
ON CONFLICT ("Id") DO NOTHING;

WITH fixture_permissions AS (
  SELECT gs, uuid_generate_v5(uuid_ns_url(), 'aspnet-starter-capacity-permission-' || gs::text) AS id
  FROM generate_series(1, :'permissions'::int) AS gs
)
INSERT INTO "Permissions" ("Id", "Name", "Description", "IsDeleted", "CreatedAt", "UpdatedAt", "DeletedAt")
SELECT id, 'capacity.permission.' || gs, 'Non-production capacity fixture permission ' || gs,
  false, now(), now(), null
FROM fixture_permissions
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO "UserRoles" ("Id", "UserId", "RoleId")
SELECT uuid_generate_v5(uuid_ns_url(), 'aspnet-starter-capacity-user-role-' || u.gs::text),
  uuid_generate_v5(uuid_ns_url(), 'aspnet-starter-capacity-user-' || u.gs::text),
  uuid_generate_v5(uuid_ns_url(), 'aspnet-starter-capacity-role-' || (((u.gs - 1) % :'roles'::int) + 1)::text)
FROM generate_series(1, :'users'::int) AS u(gs)
ON CONFLICT ("UserId", "RoleId") DO NOTHING;

INSERT INTO "RolePermission" ("RoleId", "PermissionId")
SELECT uuid_generate_v5(uuid_ns_url(), 'aspnet-starter-capacity-role-' || r.gs::text),
  uuid_generate_v5(uuid_ns_url(), 'aspnet-starter-capacity-permission-' || p.gs::text)
FROM generate_series(1, :'roles'::int) AS r(gs)
CROSS JOIN generate_series(1, :'permissions'::int) AS p(gs)
WHERE NOT EXISTS (
  SELECT 1
  FROM "RolePermission" AS existing
  WHERE existing."RoleId" = uuid_generate_v5(uuid_ns_url(), 'aspnet-starter-capacity-role-' || r.gs::text)
    AND existing."PermissionId" = uuid_generate_v5(uuid_ns_url(), 'aspnet-starter-capacity-permission-' || p.gs::text)
);
