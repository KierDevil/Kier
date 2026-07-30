using Microsoft.AspNetCore.Mvc;

namespace DepartmentFinancialRecords.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get() => Ok(new { Status = "Running", Timestamp = DateTime.UtcNow });
    }
}
