using DepartmentFinancialRecords.API.Data;
using DepartmentFinancialRecords.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DepartmentFinancialRecords.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/activity")]
    public class ActivityLogsController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;

        public ActivityLogsController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ActivityLogDto>>> Get()
        {
            var records = await _dbContext.ActivityLogs
                .OrderByDescending(item => item.Timestamp)
                .Take(300)
                .Select(item => new ActivityLogDto(item.Id, item.UserName, item.Action, item.Timestamp, item.Details))
                .ToListAsync();

            return Ok(records);
        }

        [HttpPost]
        public async Task<ActionResult<ActivityLogDto>> Create(CreateActivityLogRequest request)
        {
            var action = (request.Action ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(action)) return BadRequest(new { message = "Activity action is required." });

            var record = new ActivityLog
            {
                UserName = User.Identity?.Name ?? string.Empty,
                Action = action,
                Timestamp = DateTime.UtcNow,
                Details = request.Details?.Trim() ?? string.Empty
            };
            _dbContext.ActivityLogs.Add(record);
            await _dbContext.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new ActivityLogDto(
                record.Id,
                record.UserName,
                record.Action,
                record.Timestamp,
                record.Details));
        }
    }

    public record CreateActivityLogRequest(string Action, string? Details);
    public record ActivityLogDto(int Id, string UserName, string Action, DateTime Timestamp, string Details);
}