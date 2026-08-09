using DepartmentFinancialRecords.API.Data;
using DepartmentFinancialRecords.API.Models;
using DepartmentFinancialRecords.API.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DepartmentFinancialRecords.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class StudentsController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;

        public StudentsController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<StudentDto>>> Get()
        {
            var students = await _dbContext.Students
                .Where(student => student.IsActive)
                .OrderBy(student => student.LastName)
                .ThenBy(student => student.FirstName)
                .Select(student => StudentDto.FromStudent(student))
                .ToListAsync();

            return Ok(students);
        }

        [HttpPost]
        public async Task<ActionResult<StudentDto>> Create(CreateStudentRequest request)
        {
            var student = new Student
            {
                StudentId = request.StudentNo.Trim(),
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Course = request.Course.Trim(),
                YearLevel = request.YearLevel.Trim(),
                ContactNumber = request.ContactNumber.Trim(),
                Email = request.Email?.Trim() ?? string.Empty,
                RfidUid = RfidUtility.Normalize(request.RfidUid),
                IsActive = true
            };

            _dbContext.Students.Add(student);
            await _dbContext.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = student.Id }, StudentDto.FromStudent(student));
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<StudentDto>> Update(int id, CreateStudentRequest request)
        {
            var student = await _dbContext.Students.FirstOrDefaultAsync(item => item.Id == id && item.IsActive);
            if (student is null)
            {
                return NotFound(new { message = "Student was not found." });
            }

            student.StudentId = request.StudentNo.Trim();
            student.FirstName = request.FirstName.Trim();
            student.LastName = request.LastName.Trim();
            student.Course = request.Course.Trim();
            student.YearLevel = request.YearLevel.Trim();
            student.ContactNumber = request.ContactNumber.Trim();
            student.Email = request.Email?.Trim() ?? string.Empty;
            student.RfidUid = RfidUtility.Normalize(request.RfidUid);

            await _dbContext.SaveChangesAsync();

            return Ok(StudentDto.FromStudent(student));
        }

        [HttpGet("{id:int}/account-summary")]
        public async Task<ActionResult<StudentAccountSummaryDto>> GetAccountSummary(int id)
        {
            var student = await _dbContext.Students.FirstOrDefaultAsync(item => item.Id == id && item.IsActive);
            if (student is null)
            {
                return NotFound(new { message = "Student was not found." });
            }

            var totalFines = await _dbContext.Fines
                .Where(fine => fine.StudentId == id)
                .SumAsync(fine => (decimal?)fine.Amount) ?? 0m;

            var unpaidFinesTotal = await _dbContext.Fines
                .Where(fine => fine.StudentId == id && !fine.IsPaid)
                .SumAsync(fine => (decimal?)fine.Amount) ?? 0m;

            var totalCollections = await _dbContext.Collections
                .Where(collection => collection.StudentId == id)
                .SumAsync(collection => (decimal?)collection.AmountPaid) ?? 0m;

            var totalCollectibles = await _dbContext.Collectibles
                .Where(collectible => collectible.StudentId == id)
                .SumAsync(collectible => (decimal?)collectible.AmountDue) ?? 0m;

            var unpaidCollectiblesTotal = await _dbContext.Collectibles
                .Where(collectible => collectible.StudentId == id && !collectible.IsPaid)
                .SumAsync(collectible => (decimal?)collectible.AmountDue) ?? 0m;

            var unpaidCollectibles = await _dbContext.Collectibles
                .Where(collectible => collectible.StudentId == id && !collectible.IsPaid)
                .CountAsync();

            var unpaidFines = await _dbContext.Fines
                .Where(fine => fine.StudentId == id && !fine.IsPaid)
                .CountAsync();

            var totalOutstanding = Math.Max((unpaidFinesTotal + unpaidCollectiblesTotal) - totalCollections, 0m);

            return Ok(new StudentAccountSummaryDto(
                student.Id,
                student.StudentId,
                $"{student.FirstName} {student.LastName}".Trim(),
                totalFines,
                totalCollections,
                totalCollectibles,
                totalOutstanding,
                unpaidFines,
                unpaidCollectibles));
        }
    }

    public record CreateStudentRequest(
        string StudentNo,
        string FirstName,
        string LastName,
        string Course,
        string YearLevel,
        string ContactNumber,
        string? Email,
        string? RfidUid);

    public record StudentDto(
        int Id,
        string StudentNo,
        string FirstName,
        string LastName,
        string Name,
        string Course,
        string YearLevel,
        string ContactNumber,
        string Email,
        string RfidUid)
    {
        public static StudentDto FromStudent(Student student)
        {
            var name = $"{student.FirstName} {student.LastName}".Trim();

            return new StudentDto(
                student.Id,
                student.StudentId,
                student.FirstName,
                student.LastName,
                name,
                student.Course,
                student.YearLevel,
                student.ContactNumber,
                student.Email,
                student.RfidUid);
        }
    }

    public record StudentAccountSummaryDto(
        int StudentId,
        string StudentNo,
        string StudentName,
        decimal TotalFines,
        decimal TotalCollections,
        decimal TotalCollectibles,
        decimal OutstandingBalance,
        int UnpaidFineCount,
        int UnpaidCollectibleCount);
}
