using Microsoft.AspNetCore.Mvc;
using AspNetCoreApiStarter.Data;
using Microsoft.EntityFrameworkCore;

namespace AspNetCoreApiStarter.Controllers
{
    [Route("api/v1/health")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        private readonly ShopDbContext _dbContext;
        
        public HealthController(ShopDbContext dbContext)
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
                return StatusCode(500, new { status = "Unhealthy", db = "Failed", timestamp = DateTime.UtcNow });
            }
        }
    }
}