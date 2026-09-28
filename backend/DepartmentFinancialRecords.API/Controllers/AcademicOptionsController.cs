using DepartmentFinancialRecords.API.Data;
using DepartmentFinancialRecords.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DepartmentFinancialRecords.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/courses")]
    public class CoursesController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;

        public CoursesController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AcademicOptionDto>>> Get()
        {
            var students = await _dbContext.Students.ToListAsync();
            var courses = await _dbContext.Courses.ToListAsync();
            var changed = false;

            foreach (var name in students.Select(student => student.Course.Trim()).Where(name => name.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (courses.Any(course => string.Equals(course.Name, name, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var course = new Course { Name = name };
                courses.Add(course);
                _dbContext.Courses.Add(course);
                changed = true;
            }

            if (changed)
            {
                await _dbContext.SaveChangesAsync();
                changed = false;
            }

            foreach (var student in students.Where(student => student.CourseId is null && !string.IsNullOrWhiteSpace(student.Course)))
            {
                student.CourseId = courses.First(course => string.Equals(course.Name, student.Course.Trim(), StringComparison.OrdinalIgnoreCase)).Id;
                changed = true;
            }

            if (changed)
            {
                await _dbContext.SaveChangesAsync();
            }

            return Ok(courses.OrderBy(course => course.Name).Select(course => new AcademicOptionDto(course.Id, course.Name)));
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,Treasurer,Officer")]
        public async Task<ActionResult<AcademicOptionDto>> Create(AcademicOptionRequest request)
        {
            var name = request.Name.Trim();
            if (string.IsNullOrWhiteSpace(name)) return BadRequest(new { message = "Course name is required." });
            if (await _dbContext.Courses.AnyAsync(course => course.Name == name)) return Conflict(new { message = "Course already exists." });

            var course = new Course { Name = name };
            _dbContext.Courses.Add(course);
            await _dbContext.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new AcademicOptionDto(course.Id, course.Name));
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Administrator,Treasurer,Officer")]
        public async Task<ActionResult<AcademicOptionDto>> Update(int id, AcademicOptionRequest request)
        {
            var course = await _dbContext.Courses.FirstOrDefaultAsync(item => item.Id == id);
            if (course is null) return NotFound(new { message = "Course was not found." });
            var name = request.Name.Trim();
            if (string.IsNullOrWhiteSpace(name)) return BadRequest(new { message = "Course name is required." });
            if (await _dbContext.Courses.AnyAsync(item => item.Id != id && item.Name == name)) return Conflict(new { message = "Course already exists." });

            course.Name = name;
            foreach (var student in await _dbContext.Students.Where(student => student.CourseId == id).ToListAsync())
            {
                student.Course = name;
            }
            await _dbContext.SaveChangesAsync();
            return Ok(new AcademicOptionDto(course.Id, course.Name));
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Administrator,Treasurer,Officer")]
        public async Task<IActionResult> Delete(int id)
        {
            var course = await _dbContext.Courses.FirstOrDefaultAsync(item => item.Id == id);
            if (course is null) return NotFound(new { message = "Course was not found." });
            if (await _dbContext.Students.AnyAsync(student => student.CourseId == id))
            {
                return Conflict(new { message = "Move students to another course before deleting this option." });
            }

            _dbContext.Courses.Remove(course);
            await _dbContext.SaveChangesAsync();
            return NoContent();
        }
    }

    [ApiController]
    [Authorize]
    [Route("api/year-levels")]
    public class YearLevelsController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;

        public YearLevelsController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AcademicOptionDto>>> Get()
        {
            var students = await _dbContext.Students.ToListAsync();
            var yearLevels = await _dbContext.YearLevels.ToListAsync();
            var changed = false;

            foreach (var name in students.Select(student => student.YearLevel.Trim()).Where(name => name.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (yearLevels.Any(year => string.Equals(year.Name, name, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var yearLevel = new YearLevel { Name = name };
                yearLevels.Add(yearLevel);
                _dbContext.YearLevels.Add(yearLevel);
                changed = true;
            }

            if (changed)
            {
                await _dbContext.SaveChangesAsync();
                changed = false;
            }

            foreach (var student in students.Where(student => student.YearLevelId is null && !string.IsNullOrWhiteSpace(student.YearLevel)))
            {
                student.YearLevelId = yearLevels.First(year => string.Equals(year.Name, student.YearLevel.Trim(), StringComparison.OrdinalIgnoreCase)).Id;
                changed = true;
            }

            if (changed)
            {
                await _dbContext.SaveChangesAsync();
            }

            return Ok(yearLevels.OrderBy(year => year.Name).Select(year => new AcademicOptionDto(year.Id, year.Name)));
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,Treasurer,Officer")]
        public async Task<ActionResult<AcademicOptionDto>> Create(AcademicOptionRequest request)
        {
            var name = request.Name.Trim();
            if (string.IsNullOrWhiteSpace(name)) return BadRequest(new { message = "Year level name is required." });
            if (await _dbContext.YearLevels.AnyAsync(year => year.Name == name)) return Conflict(new { message = "Year level already exists." });

            var yearLevel = new YearLevel { Name = name };
            _dbContext.YearLevels.Add(yearLevel);
            await _dbContext.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new AcademicOptionDto(yearLevel.Id, yearLevel.Name));
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Administrator,Treasurer,Officer")]
        public async Task<ActionResult<AcademicOptionDto>> Update(int id, AcademicOptionRequest request)
        {
            var yearLevel = await _dbContext.YearLevels.FirstOrDefaultAsync(item => item.Id == id);
            if (yearLevel is null) return NotFound(new { message = "Year level was not found." });
            var name = request.Name.Trim();
            if (string.IsNullOrWhiteSpace(name)) return BadRequest(new { message = "Year level name is required." });
            if (await _dbContext.YearLevels.AnyAsync(item => item.Id != id && item.Name == name)) return Conflict(new { message = "Year level already exists." });

            yearLevel.Name = name;
            foreach (var student in await _dbContext.Students.Where(student => student.YearLevelId == id).ToListAsync())
            {
                student.YearLevel = name;
            }
            await _dbContext.SaveChangesAsync();
            return Ok(new AcademicOptionDto(yearLevel.Id, yearLevel.Name));
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Administrator,Treasurer,Officer")]
        public async Task<IActionResult> Delete(int id)
        {
            var yearLevel = await _dbContext.YearLevels.FirstOrDefaultAsync(item => item.Id == id);
            if (yearLevel is null) return NotFound(new { message = "Year level was not found." });
            if (await _dbContext.Students.AnyAsync(student => student.YearLevelId == id))
            {
                return Conflict(new { message = "Move students to another year level before deleting this option." });
            }

            _dbContext.YearLevels.Remove(yearLevel);
            await _dbContext.SaveChangesAsync();
            return NoContent();
        }
    }

    public record AcademicOptionRequest(string Name);
    public record AcademicOptionDto(int Id, string Name);
}