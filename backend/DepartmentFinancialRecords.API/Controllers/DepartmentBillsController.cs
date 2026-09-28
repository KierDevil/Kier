using DepartmentFinancialRecords.API.Data;
using DepartmentFinancialRecords.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DepartmentFinancialRecords.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/department-bills")]
    public class DepartmentBillsController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;

        public DepartmentBillsController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DepartmentBillDto>>> Get()
        {
            var bills = await _dbContext.DepartmentBills
                .Where(bill => bill.IsActive)
                .OrderByDescending(bill => bill.CreatedAt)
                .Select(bill => new DepartmentBillDto(
                    bill.Id,
                    bill.Category,
                    bill.Amount,
                    bill.CreatedAt,
                    bill.IsActive,
                    _dbContext.Collectibles.Count(item => item.DepartmentBillId == bill.Id)))
                .ToListAsync();

            return Ok(bills);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,Treasurer,Officer")]
        public async Task<ActionResult<DepartmentBillDto>> Create(CreateDepartmentBillRequest request)
        {
            var category = (request.Category ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(category))
            {
                return BadRequest(new { message = "Bill category is required." });
            }
            if (request.Amount <= 0)
            {
                return BadRequest(new { message = "Bill amount must be greater than zero." });
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            var bill = new DepartmentBill { Category = category, Amount = request.Amount };
            _dbContext.DepartmentBills.Add(bill);
            await _dbContext.SaveChangesAsync();

            var students = await _dbContext.Students
                .Where(student => student.IsActive)
                .Select(student => student.Id)
                .ToListAsync();
            _dbContext.Collectibles.AddRange(students.Select(studentId => new Collectible
            {
                StudentId = studentId,
                DepartmentBillId = bill.Id,
                Description = bill.Category,
                AmountDue = bill.Amount,
                DueDate = DateTime.UtcNow,
                IsPaid = false
            }));
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return CreatedAtAction(nameof(Get), new DepartmentBillDto(
                bill.Id,
                bill.Category,
                bill.Amount,
                bill.CreatedAt,
                bill.IsActive,
                students.Count));
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Administrator,Treasurer,Officer")]
        public async Task<ActionResult<DepartmentBillDto>> Update(int id, CreateDepartmentBillRequest request)
        {
            var bill = await _dbContext.DepartmentBills.FirstOrDefaultAsync(item => item.Id == id && item.IsActive);
            if (bill is null)
            {
                return NotFound(new { message = "Department bill was not found." });
            }
            var category = (request.Category ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(category) || request.Amount <= 0)
            {
                return BadRequest(new { message = "Bill category and a positive amount are required." });
            }

            bill.Category = category;
            bill.Amount = request.Amount;
            var outstanding = await _dbContext.Collectibles
                .Where(item => item.DepartmentBillId == bill.Id && !item.IsPaid)
                .ToListAsync();
            foreach (var item in outstanding)
            {
                item.Description = bill.Category;
                item.AmountDue = bill.Amount;
            }
            await _dbContext.SaveChangesAsync();

            var assignedCount = await _dbContext.Collectibles.CountAsync(item => item.DepartmentBillId == bill.Id);
            return Ok(new DepartmentBillDto(bill.Id, bill.Category, bill.Amount, bill.CreatedAt, bill.IsActive, assignedCount));
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Administrator,Treasurer,Officer")]
        public async Task<IActionResult> Delete(int id)
        {
            var bill = await _dbContext.DepartmentBills.FirstOrDefaultAsync(item => item.Id == id && item.IsActive);
            if (bill is null)
            {
                return NotFound(new { message = "Department bill was not found." });
            }

            bill.IsActive = false;
            await _dbContext.SaveChangesAsync();
            return NoContent();
        }
    }

    public record CreateDepartmentBillRequest(string Category, decimal Amount);
    public record DepartmentBillDto(int Id, string Category, decimal Amount, DateTime CreatedAt, bool IsActive, int AssignedCount);
}