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
    public class DisbursementsController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;

        public DisbursementsController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DisbursementDto>>> Get()
        {
            var records = await _dbContext.Disbursements
                .OrderByDescending(item => item.DateReleased)
                .Select(item => new DisbursementDto(
                    item.Id,
                    item.Payee,
                    item.Amount,
                    item.DateReleased,
                    item.Purpose,
                    item.DocumentPath,
                    item.IsApproved))
                .ToListAsync();

            return Ok(records);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<DisbursementDto>> GetById(int id)
        {
            var record = await _dbContext.Disbursements.FirstOrDefaultAsync(item => item.Id == id);
            if (record is null)
            {
                return NotFound(new { message = "Disbursement record not found." });
            }

            return Ok(new DisbursementDto(
                record.Id,
                record.Payee,
                record.Amount,
                record.DateReleased,
                record.Purpose,
                record.DocumentPath,
                record.IsApproved));
        }

        [HttpPost]
        public async Task<ActionResult<DisbursementDto>> Create(CreateDisbursementRequest request)
        {
            var record = new Disbursement
            {
                Payee = request.Payee.Trim(),
                Amount = request.Amount,
                DateReleased = request.DateReleased ?? DateTime.UtcNow,
                Purpose = request.Purpose?.Trim() ?? string.Empty,
                DocumentPath = request.DocumentPath?.Trim() ?? string.Empty,
                IsApproved = request.IsApproved ?? false
            };

            _dbContext.Disbursements.Add(record);
            await _dbContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = record.Id }, new DisbursementDto(
                record.Id,
                record.Payee,
                record.Amount,
                record.DateReleased,
                record.Purpose,
                record.DocumentPath,
                record.IsApproved));
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<DisbursementDto>> Update(int id, CreateDisbursementRequest request)
        {
            var record = await _dbContext.Disbursements.FirstOrDefaultAsync(item => item.Id == id);
            if (record is null)
            {
                return NotFound(new { message = "Disbursement record not found." });
            }

            record.Payee = request.Payee.Trim();
            record.Amount = request.Amount;
            record.DateReleased = request.DateReleased ?? record.DateReleased;
            record.Purpose = request.Purpose?.Trim() ?? record.Purpose;
            record.DocumentPath = request.DocumentPath?.Trim() ?? record.DocumentPath;
            record.IsApproved = request.IsApproved ?? record.IsApproved;

            await _dbContext.SaveChangesAsync();

            return Ok(new DisbursementDto(
                record.Id,
                record.Payee,
                record.Amount,
                record.DateReleased,
                record.Purpose,
                record.DocumentPath,
                record.IsApproved));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var record = await _dbContext.Disbursements.FirstOrDefaultAsync(item => item.Id == id);
            if (record is null)
            {
                return NotFound(new { message = "Disbursement record not found." });
            }

            _dbContext.Disbursements.Remove(record);
            await _dbContext.SaveChangesAsync();

            return NoContent();
        }
    }

    public record CreateDisbursementRequest(
        string Payee,
        decimal Amount,
        DateTime? DateReleased,
        string? Purpose,
        string? DocumentPath,
        bool? IsApproved);

    public record DisbursementDto(
        int Id,
        string Payee,
        decimal Amount,
        DateTime DateReleased,
        string Purpose,
        string DocumentPath,
        bool IsApproved);
}
