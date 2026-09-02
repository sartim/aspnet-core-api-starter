using System;
using Microsoft.AspNetCore.Mvc;
using AspNetCoreApiStarter.Models;
using AspNetCoreApiStarter.Data;
using Microsoft.EntityFrameworkCore;

namespace AspNetCoreApiStarter.Controllers
{
    [Route("api/v1/roles")]
    [ApiController]
    public class RoleController : BaseController<Role>
    {
        public RoleController(ApplicationDbContext dbContext) : base(dbContext)
        {
        }
    }
}
