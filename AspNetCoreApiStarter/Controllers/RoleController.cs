using System;
using Microsoft.AspNetCore.Mvc;
using AspNetCoreApiStarter.Models;
using AspNetCoreApiStarter.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using AspNetCoreApiStarter.Authorization;

namespace AspNetCoreApiStarter.Controllers
{
    [Route("api/v1/roles")]
    [ApiController]
    [Authorize(Policy = AuthorizationPolicies.RolesManage)]
    public class RoleController : BaseController<Role>
    {
        public RoleController(ApplicationDbContext dbContext) : base(dbContext)
        {
        }
    }
}
