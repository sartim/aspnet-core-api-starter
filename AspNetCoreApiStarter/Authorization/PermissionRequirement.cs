using Microsoft.AspNetCore.Authorization;

namespace AspNetCoreApiStarter.Authorization;

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.IsInRole("Administrator") ||
            context.User.Claims.Any(claim =>
                claim.Type == AuthorizationPolicies.PermissionClaimType &&
                claim.Value.Equals(requirement.Permission, StringComparison.OrdinalIgnoreCase)))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
