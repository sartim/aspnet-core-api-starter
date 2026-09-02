using System;
using AspNetCoreApiStarter.Data;
using AspNetCoreApiStarter.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AspNetCoreApiStarter.Authorization;

namespace AspNetCoreApiStarter.Controllers
{
    [Route("api/v1/role-permissions")]
    [ApiController]
    [Authorize(Policy = AuthorizationPolicies.RolePermissionsManage)]
    public class RolePermissionController : BaseController<RolePermission>
    {
        public RolePermissionController(ApplicationDbContext dbContext) : base(dbContext)
        {
        }
    }
}
