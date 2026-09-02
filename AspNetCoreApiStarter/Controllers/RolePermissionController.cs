using System;
using AspNetCoreApiStarter.Data;
using AspNetCoreApiStarter.Models;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCoreApiStarter.Controllers
{
    [Route("api/v1/role-permissions")]
    [ApiController]
    public class RolePermissionController : BaseController<RolePermission>
    {
        public RolePermissionController(ApplicationDbContext dbContext) : base(dbContext)
        {
        }
    }
}

