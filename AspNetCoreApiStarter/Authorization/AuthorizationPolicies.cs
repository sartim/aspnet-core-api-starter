namespace AspNetCoreApiStarter.Authorization;

public static class AuthorizationPolicies
{
    public const string Administrator = "administrator";
    public const string UsersManage = "users.manage";
    public const string RolesManage = "roles.manage";
    public const string PermissionsManage = "permissions.manage";
    public const string RolePermissionsManage = "role-permissions.manage";

    public const string PermissionClaimType = "permission";
}
