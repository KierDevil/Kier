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
    public class FundTransactionsController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;

        public FundTransactionsController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<FundTransactionDto>>> Get()
        {
            var records = await _dbContext.FundTransactions
                .OrderByDescending(item => item.TransactionDate)
                .Select(item => new FundTransactionDto(
                    item.Id,
                    item.TransactionType.ToString(),
                    item.Amount,
                    item.TransactionDate,
                    item.Source,
                    item.Remarks))
                .ToListAsync();

            return Ok(records);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<FundTransactionDto>> GetById(int id)
        {
            var record = await _dbContext.FundTransactions.FirstOrDefaultAsync(item => item.Id == id);
            if (record is null)
            {
                return NotFound(new { message = "Fund transaction not found." });
            }

            return Ok(new FundTransactionDto(
                record.Id,
                record.TransactionType.ToString(),
                record.Amount,
                record.TransactionDate,
                record.Source,
                record.Remarks));
        }

        [HttpPost]
        public async Task<ActionResult<FundTransactionDto>> Create(CreateFundTransactionRequest request)
        {
            if (!Enum.TryParse<FundTransactionType>(request.TransactionType, true, out var transactionType))
            {
                return BadRequest(new { message = "Invalid transaction type. Supported values: BeginningBalance, Addition, Deduction." });
            }

            var record = new FundTransaction
            {
                TransactionType = transactionType,
                Amount = request.Amount,
                TransactionDate = request.TransactionDate ?? DateTime.UtcNow,
                Source = request.Source?.Trim() ?? string.Empty,
                Remarks = request.Remarks?.Trim() ?? string.Empty
            };

            _dbContext.FundTransactions.Add(record);
            await _dbContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = record.Id }, new FundTransactionDto(
                record.Id,
                record.TransactionType.ToString(),
                record.Amount,
                record.TransactionDate,
                record.Source,
                record.Remarks));
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<FundTransactionDto>> Update(int id, CreateFundTransactionRequest request)
        {
            var record = await _dbContext.FundTransactions.FirstOrDefaultAsync(item => item.Id == id);
            if (record is null)
            {
                return NotFound(new { message = "Fund transaction not found." });
            }

            if (!Enum.TryParse<FundTransactionType>(request.TransactionType, true, out var transactionType))
            {
                return BadRequest(new { message = "Invalid transaction type. Supported values: BeginningBalance, Addition, Deduction." });
            }

            record.TransactionType = transactionType;
            record.Amount = request.Amount;
            record.TransactionDate = request.TransactionDate ?? record.TransactionDate;
            record.Source = request.Source?.Trim() ?? record.Source;
            record.Remarks = request.Remarks?.Trim() ?? record.Remarks;

            await _dbContext.SaveChangesAsync();

            return Ok(new FundTransactionDto(
                record.Id,
                record.TransactionType.ToString(),
                record.Amount,
                record.TransactionDate,
                record.Source,
                record.Remarks));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var record = await _dbContext.FundTransactions.FirstOrDefaultAsync(item => item.Id == id);
            if (record is null)
            {
                return NotFound(new { message = "Fund transaction not found." });
            }

            _dbContext.FundTransactions.Remove(record);
            await _dbContext.SaveChangesAsync();

            return NoContent();
        }
    }

    public record CreateFundTransactionRequest(
        string TransactionType,
        decimal Amount,
        DateTime? TransactionDate,
        string? Source,
        string? Remarks);

    public record FundTransactionDto(
        int Id,
        string TransactionType,
        decimal Amount,
        DateTime TransactionDate,
        string Source,
        string Remarks);
}
