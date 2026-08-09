using DepartmentFinancialRecords.API.Data;
using DepartmentFinancialRecords.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DepartmentFinancialRecords.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class CollectiblesController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;

        public CollectiblesController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CollectibleDto>>> Get()
        {
            var records = await _dbContext.Collectibles
                .Include(item => item.Student)
                .OrderByDescending(item => item.DueDate)
                .Select(item => new CollectibleDto(
                    item.Id,
                    item.StudentId,
                    item.Student != null ? $"{item.Student.FirstName} {item.Student.LastName}".Trim() : string.Empty,
                    item.Description,
                    item.AmountDue,
                    item.DueDate,
                    item.IsPaid))
                .ToListAsync();

            return Ok(records);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<CollectibleDto>> GetById(int id)
        {
            var record = await _dbContext.Collectibles.Include(item => item.Student).FirstOrDefaultAsync(item => item.Id == id);
            if (record is null)
            {
                return NotFound(new { message = "Collectible record not found." });
            }

            return Ok(new CollectibleDto(
                record.Id,
                record.StudentId,
                record.Student != null ? $"{record.Student.FirstName} {record.Student.LastName}".Trim() : string.Empty,
                record.Description,
                record.AmountDue,
                record.DueDate,
                record.IsPaid));
        }

        [HttpPost]
        public async Task<ActionResult<CollectibleDto>> Create(CreateCollectibleRequest request)
        {
            var studentExists = await _dbContext.Students.AnyAsync(student => student.Id == request.StudentId && student.IsActive);
            if (!studentExists)
            {
                return BadRequest(new { message = "Student was not found." });
            }

            var record = new Collectible
            {
                StudentId = request.StudentId,
                Description = request.Description.Trim(),
                AmountDue = request.AmountDue,
                DueDate = request.DueDate ?? DateTime.UtcNow,
                IsPaid = request.IsPaid ?? false
            };

            _dbContext.Collectibles.Add(record);
            await _dbContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = record.Id }, new CollectibleDto(
                record.Id,
                record.StudentId,
                string.Empty,
                record.Description,
                record.AmountDue,
                record.DueDate,
                record.IsPaid));
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<CollectibleDto>> Update(int id, CreateCollectibleRequest request)
        {
            var record = await _dbContext.Collectibles.Include(item => item.Student).FirstOrDefaultAsync(item => item.Id == id);
            if (record is null)
            {
                return NotFound(new { message = "Collectible record not found." });
            }

            var studentExists = await _dbContext.Students.AnyAsync(student => student.Id == request.StudentId && student.IsActive);
            if (!studentExists)
            {
                return BadRequest(new { message = "Student was not found." });
            }

            record.StudentId = request.StudentId;
            record.Description = request.Description.Trim();
            record.AmountDue = request.AmountDue;
            record.DueDate = request.DueDate ?? record.DueDate;
            record.IsPaid = request.IsPaid ?? record.IsPaid;

            await _dbContext.SaveChangesAsync();

            return Ok(new CollectibleDto(
                record.Id,
                record.StudentId,
                record.Student != null ? $"{record.Student.FirstName} {record.Student.LastName}".Trim() : string.Empty,
                record.Description,
                record.AmountDue,
                record.DueDate,
                record.IsPaid));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var record = await _dbContext.Collectibles.FirstOrDefaultAsync(item => item.Id == id);
            if (record is null)
            {
                return NotFound(new { message = "Collectible record not found." });
            }

            _dbContext.Collectibles.Remove(record);
            await _dbContext.SaveChangesAsync();

            return NoContent();
        }
    }

    public record CreateCollectibleRequest(
        int StudentId,
        string Description,
        decimal AmountDue,
        DateTime? DueDate,
        bool? IsPaid);

    public record CollectibleDto(
        int Id,
        int StudentId,
        string StudentName,
        string Description,
        decimal AmountDue,
        DateTime DueDate,
        bool IsPaid);
}
