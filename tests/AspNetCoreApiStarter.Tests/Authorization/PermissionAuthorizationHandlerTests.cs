using System.Security.Claims;
using AspNetCoreApiStarter.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace AspNetCoreApiStarter.Tests.Authorization;

public class PermissionAuthorizationHandlerTests
{
    [Fact]
    public async Task Handler_AllowsMatchingPermissionClaim()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(AuthorizationPolicies.PermissionClaimType, AuthorizationPolicies.UsersManage)],
            "test"));
        var requirement = new PermissionRequirement(AuthorizationPolicies.UsersManage);
        var context = new AuthorizationHandlerContext([requirement], user, null);

        await new PermissionAuthorizationHandler().HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Handler_AllowsAdministratorWithoutPermissionClaims()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, "Administrator")],
            "test"));
        var requirement = new PermissionRequirement(AuthorizationPolicies.RolePermissionsManage);
        var context = new AuthorizationHandlerContext([requirement], user, null);

        await new PermissionAuthorizationHandler().HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Handler_DeniesDifferentPermissionClaim()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(AuthorizationPolicies.PermissionClaimType, AuthorizationPolicies.RolesManage)],
            "test"));
        var requirement = new PermissionRequirement(AuthorizationPolicies.UsersManage);
        var context = new AuthorizationHandlerContext([requirement], user, null);

        await new PermissionAuthorizationHandler().HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }
}
