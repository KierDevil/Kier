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
    public class CollectionsController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;

        public CollectionsController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CollectionDto>>> Get()
        {
            var records = await _dbContext.Collections
                .Include(collection => collection.Student)
                .OrderByDescending(collection => collection.PaymentDate)
                .Select(collection => new CollectionDto(
                    collection.Id,
                    collection.StudentId,
                    collection.Student != null ? $"{collection.Student.FirstName} {collection.Student.LastName}".Trim() : string.Empty,
                    collection.AmountPaid,
                    collection.PaymentDate,
                    collection.CollectorName,
                    collection.ReceiptNumber,
                    collection.Category))
                .ToListAsync();

            return Ok(records);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<CollectionDto>> GetById(int id)
        {
            var collection = await _dbContext.Collections
                .Include(item => item.Student)
                .FirstOrDefaultAsync(item => item.Id == id);

            if (collection is null)
            {
                return NotFound(new { message = "Collection record not found." });
            }

            return Ok(new CollectionDto(
                collection.Id,
                collection.StudentId,
                collection.Student != null ? $"{collection.Student.FirstName} {collection.Student.LastName}".Trim() : string.Empty,
                collection.AmountPaid,
                collection.PaymentDate,
                collection.CollectorName,
                collection.ReceiptNumber,
                collection.Category));
        }

        [HttpPost]
        public async Task<ActionResult<CollectionDto>> Create(CreateCollectionRequest request)
        {
            var studentExists = await _dbContext.Students.AnyAsync(student => student.Id == request.StudentId && student.IsActive);
            if (!studentExists)
            {
                return BadRequest(new { message = "Student was not found." });
            }

            if (!string.IsNullOrWhiteSpace(request.ReceiptNumber))
            {
                var normalizedReceipt = request.ReceiptNumber.Trim();
                var exists = await _dbContext.Collections.AnyAsync(item => item.ReceiptNumber == normalizedReceipt && !string.IsNullOrWhiteSpace(item.ReceiptNumber));
                if (exists)
                {
                    return Conflict(new { message = "A payment record with the same receipt number already exists." });
                }
            }

            var collection = new Collection
            {
                StudentId = request.StudentId,
                AmountPaid = request.AmountPaid,
                PaymentDate = request.PaymentDate ?? DateTime.UtcNow,
                CollectorName = request.CollectorName?.Trim() ?? string.Empty,
                ReceiptNumber = request.ReceiptNumber?.Trim() ?? string.Empty,
                Category = request.Category?.Trim() ?? string.Empty
            };

            _dbContext.Collections.Add(collection);
            await _dbContext.SaveChangesAsync();
            await ReconcileStudentFinesAsync(collection.StudentId, collection.Category, collection.AmountPaid, isPaid: collection.AmountPaid > 0m);

            return CreatedAtAction(nameof(GetById), new { id = collection.Id }, new CollectionDto(
                collection.Id,
                collection.StudentId,
                string.Empty,
                collection.AmountPaid,
                collection.PaymentDate,
                collection.CollectorName,
                collection.ReceiptNumber,
                collection.Category));
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<CollectionDto>> Update(int id, CreateCollectionRequest request)
        {
            var collection = await _dbContext.Collections.Include(item => item.Student).FirstOrDefaultAsync(item => item.Id == id);
            if (collection is null)
            {
                return NotFound(new { message = "Collection record not found." });
            }

            var studentExists = await _dbContext.Students.AnyAsync(student => student.Id == request.StudentId && student.IsActive);
            if (!studentExists)
            {
                return BadRequest(new { message = "Student was not found." });
            }

            if (!string.IsNullOrWhiteSpace(request.ReceiptNumber) && request.ReceiptNumber.Trim() != collection.ReceiptNumber)
            {
                var normalizedReceipt = request.ReceiptNumber.Trim();
                var exists = await _dbContext.Collections.AnyAsync(item => item.Id != id && item.ReceiptNumber == normalizedReceipt && !string.IsNullOrWhiteSpace(item.ReceiptNumber));
                if (exists)
                {
                    return Conflict(new { message = "A payment record with the same receipt number already exists." });
                }
            }

            var previousStudentId = collection.StudentId;
            var previousAmount = collection.AmountPaid;
            var previousCategory = collection.Category;
            var previousPaid = previousAmount > 0m;

            collection.StudentId = request.StudentId;
            collection.AmountPaid = request.AmountPaid;
            collection.PaymentDate = request.PaymentDate ?? collection.PaymentDate;
            collection.CollectorName = request.CollectorName?.Trim() ?? collection.CollectorName;
            collection.ReceiptNumber = request.ReceiptNumber?.Trim() ?? collection.ReceiptNumber;
            collection.Category = request.Category?.Trim() ?? collection.Category;

            await _dbContext.SaveChangesAsync();

            if (previousStudentId != collection.StudentId || previousPaid || !string.IsNullOrWhiteSpace(previousCategory))
            {
                await ReconcileStudentFinesAsync(previousStudentId, previousCategory, previousAmount, isPaid: previousPaid, reverse: true);
            }

            await ReconcileStudentFinesAsync(collection.StudentId, collection.Category, collection.AmountPaid, isPaid: collection.AmountPaid > 0m);

            return Ok(new CollectionDto(
                collection.Id,
                collection.StudentId,
                collection.Student != null ? $"{collection.Student.FirstName} {collection.Student.LastName}".Trim() : string.Empty,
                collection.AmountPaid,
                collection.PaymentDate,
                collection.CollectorName,
                collection.ReceiptNumber,
                collection.Category));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var collection = await _dbContext.Collections.FirstOrDefaultAsync(item => item.Id == id);
            if (collection is null)
            {
                return NotFound(new { message = "Collection record not found." });
            }

            var studentId = collection.StudentId;
            var category = collection.Category;
            var amount = collection.AmountPaid;

            _dbContext.Collections.Remove(collection);
            await _dbContext.SaveChangesAsync();
            await ReconcileStudentFinesAsync(studentId, category, amount, isPaid: amount > 0m, reverse: true);

            return NoContent();
        }

        private async Task ReconcileStudentFinesAsync(int studentId, string category, decimal amount, bool isPaid, bool reverse = false)
        {
            if (studentId <= 0 || amount <= 0m)
            {
                return;
            }

            var normalizedCategory = (category ?? string.Empty).Trim();
            var isFineRelated = normalizedCategory.Contains("fine", StringComparison.OrdinalIgnoreCase)
                || normalizedCategory.Contains("late", StringComparison.OrdinalIgnoreCase)
                || normalizedCategory.Contains("absent", StringComparison.OrdinalIgnoreCase);

            if (!isFineRelated)
            {
                return;
            }

            var studentFines = await _dbContext.Fines
                .Where(fine => fine.StudentId == studentId)
                .OrderBy(fine => fine.DateIssued)
                .ToListAsync();

            if (studentFines.Count == 0)
            {
                return;
            }

            if (reverse)
            {
                foreach (var fine in studentFines.Where(fine => fine.IsPaid))
                {
                    fine.IsPaid = false;
                }

                return;
            }

            if (!isPaid)
            {
                return;
            }

            decimal remaining = amount;
            foreach (var fine in studentFines.Where(fine => !fine.IsPaid).OrderBy(fine => fine.DateIssued))
            {
                if (remaining <= 0m)
                {
                    break;
                }

                if (remaining >= fine.Amount)
                {
                    fine.IsPaid = true;
                    remaining -= fine.Amount;
                    continue;
                }

                break;
            }
        }
    }

    public record CreateCollectionRequest(
        int StudentId,
        decimal AmountPaid,
        DateTime? PaymentDate,
        string? CollectorName,
        string? ReceiptNumber,
        string? Category);

    public record CollectionDto(
        int Id,
        int StudentId,
        string StudentName,
        decimal AmountPaid,
        DateTime PaymentDate,
        string CollectorName,
        string ReceiptNumber,
        string Category);
}
