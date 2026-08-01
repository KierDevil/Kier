using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DepartmentFinancialRecords.API.Data;

namespace DepartmentFinancialRecords.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;

        public HealthController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var databaseReady = false;

            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                databaseReady = await _dbContext.Database.CanConnectAsync(timeout.Token);
            }
            catch
            {
                databaseReady = false;
            }

            return Ok(new
            {
                status = "Running",
                database = databaseReady ? "Connected" : "Unavailable",
                timestamp = DateTime.UtcNow
            });
        }
    }
}
