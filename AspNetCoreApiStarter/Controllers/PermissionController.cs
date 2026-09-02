using System;
using Microsoft.AspNetCore.Mvc;
using AspNetCoreApiStarter.Models;
using AspNetCoreApiStarter.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using AspNetCoreApiStarter.Authorization;

namespace AspNetCoreApiStarter.Controllers
{
    [Route("api/v1/permissions")]
    [ApiController]
    [Authorize(Policy = AuthorizationPolicies.PermissionsManage)]
    public class PermissionController : BaseController<Permission>
    {
        public PermissionController(ApplicationDbContext dbContext) : base(dbContext)
        {
        }
    }
}
