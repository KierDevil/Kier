using DepartmentFinancialRecords.API.Data;
using DepartmentFinancialRecords.API.Models;
using DepartmentFinancialRecords.API.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DepartmentFinancialRecords.API.Controllers
{
    [ApiController]
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
                Section = request.Section.Trim(),
                ContactNumber = request.ContactNumber.Trim(),
                RfidUid = RfidUtility.Normalize(request.RfidUid),
                IsActive = true
            };

            _dbContext.Students.Add(student);
            await _dbContext.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = student.Id }, StudentDto.FromStudent(student));
        }
    }

    public record CreateStudentRequest(
        string StudentNo,
        string FirstName,
        string LastName,
        string Course,
        string Section,
        string ContactNumber,
        string? RfidUid);

    public record StudentDto(
        int Id,
        string StudentNo,
        string FirstName,
        string LastName,
        string Name,
        string Course,
        string Section,
        string ContactNumber,
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
                student.Section,
                student.ContactNumber,
                student.RfidUid);
        }
    }
}
