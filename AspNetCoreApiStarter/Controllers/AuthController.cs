using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AspNetCoreApiStarter.Data;
using AspNetCoreApiStarter.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using AspNetCoreApiStarter.Observability;
using AspNetCoreApiStarter.Authorization;

namespace AspNetCoreApiStarter.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("generate-jwt")]
        public IActionResult GenerateToken(Login login)
        {
            // find user by email
            var user = _context.Users.Include(u => u.UserRoles).ThenInclude(userRole => userRole.Role)
                .ThenInclude(role => role.RolePermissions).ThenInclude(rolePermission => rolePermission.Permission)
                .FirstOrDefault(u => u.Email == login.Email && u.IsActive);

            // check if user exists and password matches
            if (user == null || !VerifyPassword(login.Password, user.Password))
            {
                return new ObjectResult(new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Unauthorized",
                    Detail = "The supplied email or password is incorrect.",
                    Instance = HttpContext.Request.Path,
                    Extensions = { ["traceId"] = StarterProblemDetails.GetTraceId(HttpContext) }
                })
                {
                    StatusCode = StatusCodes.Status401Unauthorized,
                    ContentTypes = { "application/problem+json" }
                };

            }

            // generate token
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(
                Environment.GetEnvironmentVariable("JWT_SECRET_KEY"));
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                new Claim(ClaimTypes.Email, login.Email)
                }
                .Concat(user.UserRoles.Select(userRole => new Claim(ClaimTypes.Role, userRole.Role.Name)))
                .Concat(user.UserRoles.SelectMany(userRole => userRole.Role.RolePermissions)
                    .Select(rolePermission => new Claim(AuthorizationPolicies.PermissionClaimType, rolePermission.Permission.Name)))
                ),
                Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER"),
                Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
                Expires = DateTime.UtcNow.AddMinutes(
                    int.Parse(Environment.GetEnvironmentVariable("JWT_EXPIRY"))),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var roles = user.UserRoles.Select(userRole => userRole.Role.Name).ToArray();

            return new JsonResult(new
            {
                Token = tokenHandler.WriteToken(token),
                User = new
                {
                    FirstName = user.FirstName,
                    Email = user.Email,
                    Roles = roles
                }
            })
            { StatusCode = 200 };
        }

        private bool VerifyPassword(string password, string passwordHash)
        {
            try
            {
                byte[] passwordHashBytes = Encoding.UTF8.GetBytes(passwordHash);
                string hashedPassword = Encoding.UTF8.GetString(passwordHashBytes);
                return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occurred during password verification: " + ex.Message);
                return false;
            }
        }
    }
}
