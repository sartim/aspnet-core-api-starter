using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AspNetCoreApiStarter.Data;
using Microsoft.EntityFrameworkCore;
using AspNetCoreApiStarter.Observability;

namespace AspNetCoreApiStarter.Controllers
{
    [Route("api/v1/health")]
    [ApiController]
    [AllowAnonymous]
    public class HealthController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;

        public HealthController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        // GET api/v1/health
        [HttpGet]
        public IActionResult Get()
        {
            try
            {
                _dbContext.Database.ExecuteSqlRaw("SELECT 1");
                return Ok(new { status = "Healthy", db = "OK", timestamp = DateTime.UtcNow });
            }
            catch
            {
                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status503ServiceUnavailable,
                    Title = "Service Unavailable",
                    Detail = "The database health check failed.",
                    Instance = HttpContext.Request.Path
                };
                problem.Extensions["traceId"] = StarterProblemDetails.GetTraceId(HttpContext);
                return StatusCode(StatusCodes.Status503ServiceUnavailable, problem);
            }
        }
    }
}
