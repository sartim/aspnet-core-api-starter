using System;
using Microsoft.AspNetCore.Mvc;
using AspNetCoreApiStarter.Models;
using AspNetCoreApiStarter.Data;
using Microsoft.EntityFrameworkCore;

namespace AspNetCoreApiStarter.Controllers
{
    [Route("api/v1/permissions")]
    [ApiController]
    public class PermissionController : BaseController<Permission>
    {
        public PermissionController(ApplicationDbContext dbContext) : base(dbContext)
        {
        }
    }
}
