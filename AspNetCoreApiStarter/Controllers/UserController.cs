using System;
using Microsoft.AspNetCore.Mvc;
using AspNetCoreApiStarter.Models;
using AspNetCoreApiStarter.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using AspNetCoreApiStarter.Authorization;

namespace AspNetCoreApiStarter.Controllers
{
    [Route("api/v1/users")]
    [ApiController]
    [Authorize(Policy = AuthorizationPolicies.UsersManage)]
    public class UserController : BaseController<User>
    {

        public UserController(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        // POST: api/user
        [HttpPost]
        public override async Task<ActionResult<User>> Post(User user)
        {
            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
            return await base.Post(user);
        }

    }
}
