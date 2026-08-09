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
    public class FinesController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;

        public FinesController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<FineDto>>> Get()
        {
            var fines = await _dbContext.Fines
                .Include(fine => fine.Student)
                .OrderByDescending(fine => fine.DateIssued)
                .Select(fine => new FineDto(
                    fine.Id,
                    fine.StudentId,
                    fine.Student != null ? $"{fine.Student.FirstName} {fine.Student.LastName}".Trim() : string.Empty,
                    fine.Category,
                    fine.Amount,
                    fine.DateIssued,
                    fine.Remarks,
                    fine.IsPaid))
                .ToListAsync();

            return Ok(fines);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<FineDto>> GetById(int id)
        {
            var fine = await _dbContext.Fines
                .Include(item => item.Student)
                .FirstOrDefaultAsync(item => item.Id == id);

            if (fine is null)
            {
                return NotFound(new { message = "Fine record not found." });
            }

            return Ok(new FineDto(
                fine.Id,
                fine.StudentId,
                fine.Student != null ? $"{fine.Student.FirstName} {fine.Student.LastName}".Trim() : string.Empty,
                fine.Category,
                fine.Amount,
                fine.DateIssued,
                fine.Remarks,
                fine.IsPaid));
        }

        [HttpPost]
        public async Task<ActionResult<FineDto>> Create(CreateFineRequest request)
        {
            var studentExists = await _dbContext.Students.AnyAsync(student => student.Id == request.StudentId && student.IsActive);
            if (!studentExists)
            {
                return BadRequest(new { message = "Student was not found." });
            }

            var fine = new Fine
            {
                StudentId = request.StudentId,
                Category = request.Category.Trim(),
                Amount = request.Amount,
                DateIssued = request.DateIssued ?? DateTime.UtcNow,
                Remarks = request.Remarks ?? string.Empty,
                IsPaid = request.IsPaid ?? false
            };

            _dbContext.Fines.Add(fine);
            await _dbContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = fine.Id }, new FineDto(
                fine.Id,
                fine.StudentId,
                string.Empty,
                fine.Category,
                fine.Amount,
                fine.DateIssued,
                fine.Remarks,
                fine.IsPaid));
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<FineDto>> Update(int id, CreateFineRequest request)
        {
            var fine = await _dbContext.Fines
                .Include(item => item.Student)
                .FirstOrDefaultAsync(item => item.Id == id);

            if (fine is null)
            {
                return NotFound(new { message = "Fine record not found." });
            }

            var studentExists = await _dbContext.Students.AnyAsync(student => student.Id == request.StudentId && student.IsActive);
            if (!studentExists)
            {
                return BadRequest(new { message = "Student was not found." });
            }

            fine.StudentId = request.StudentId;
            fine.Category = request.Category.Trim();
            fine.Amount = request.Amount;
            fine.DateIssued = request.DateIssued ?? fine.DateIssued;
            fine.Remarks = request.Remarks ?? fine.Remarks;
            fine.IsPaid = request.IsPaid ?? fine.IsPaid;

            await _dbContext.SaveChangesAsync();

            return Ok(new FineDto(
                fine.Id,
                fine.StudentId,
                fine.Student != null ? $"{fine.Student.FirstName} {fine.Student.LastName}".Trim() : string.Empty,
                fine.Category,
                fine.Amount,
                fine.DateIssued,
                fine.Remarks,
                fine.IsPaid));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var fine = await _dbContext.Fines.FirstOrDefaultAsync(item => item.Id == id);
            if (fine is null)
            {
                return NotFound(new { message = "Fine record not found." });
            }

            _dbContext.Fines.Remove(fine);
            await _dbContext.SaveChangesAsync();

            return NoContent();
        }
    }

    public record CreateFineRequest(
        int StudentId,
        string Category,
        decimal Amount,
        DateTime? DateIssued,
        string? Remarks,
        bool? IsPaid);

    public record FineDto(
        int Id,
        int StudentId,
        string StudentName,
        string Category,
        decimal Amount,
        DateTime DateIssued,
        string Remarks,
        bool IsPaid);
}
