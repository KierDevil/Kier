using DepartmentFinancialRecords.API.Data;
using DepartmentFinancialRecords.API.Models;
using DepartmentFinancialRecords.API.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DepartmentFinancialRecords.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AttendanceController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;

        public AttendanceController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AttendanceRecordDto>>> Get()
        {
            var records = await _dbContext.AttendanceRecords
                .Include(record => record.Student)
                .Include(record => record.AttendanceEvent)
                .OrderByDescending(record => record.RecordedAt)
                .Take(250)
                .Select(record => AttendanceRecordDto.FromRecord(record))
                .ToListAsync();

            return Ok(records);
        }

        [HttpPost("scan")]
        public async Task<ActionResult<AttendanceRecordDto>> Scan(ScanAttendanceRequest request)
        {
            var studentNo = request.StudentNo?.Trim() ?? string.Empty;
            var rfidUid = RfidUtility.Normalize(request.RfidUid);
            var student = await _dbContext.Students.FirstOrDefaultAsync(item =>
                item.IsActive &&
                ((!string.IsNullOrWhiteSpace(studentNo) && item.StudentId == studentNo) ||
                 (!string.IsNullOrWhiteSpace(rfidUid) && item.RfidUid == rfidUid)));
            if (student is null)
            {
                return NotFound(new { message = $"No student was found for ID/RFID {studentNo}{rfidUid}." });
            }

            var title = string.IsNullOrWhiteSpace(request.EventTitle) ? "Attendance Scan" : request.EventTitle.Trim();
            var attendanceEvent = await _dbContext.AttendanceEvents
                .FirstOrDefaultAsync(item => item.Title == title && item.EventDate.Date == DateTime.UtcNow.Date);

            if (attendanceEvent is null)
            {
                attendanceEvent = new AttendanceEvent
                {
                    Title = title,
                    EventDate = DateTime.UtcNow,
                    Location = request.Location?.Trim() ?? string.Empty,
                    Description = "Created from QR/RFID attendance scan."
                };
                _dbContext.AttendanceEvents.Add(attendanceEvent);
                await _dbContext.SaveChangesAsync();
            }

            var status = Enum.TryParse<AttendanceStatus>(request.Status, true, out var parsedStatus)
                ? parsedStatus
                : AttendanceStatus.Present;

            var existingRecord = await _dbContext.AttendanceRecords
                .Include(record => record.Student)
                .Include(record => record.AttendanceEvent)
                .FirstOrDefaultAsync(record =>
                    record.StudentId == student.Id &&
                    record.AttendanceEventId == attendanceEvent.Id);

            if (existingRecord is not null)
            {
                existingRecord.Status = status;
                existingRecord.RecordedAt = DateTime.UtcNow;
                existingRecord.Remarks = request.Remarks?.Trim() ?? "Updated by QR/RFID scan.";
                await _dbContext.SaveChangesAsync();

                return Ok(AttendanceRecordDto.FromRecord(existingRecord));
            }

            var record = new AttendanceRecord
            {
                StudentId = student.Id,
                AttendanceEventId = attendanceEvent.Id,
                Status = status,
                RecordedAt = DateTime.UtcNow,
                Remarks = request.Remarks?.Trim() ?? "Recorded by QR/RFID scan."
            };

            _dbContext.AttendanceRecords.Add(record);
            await _dbContext.SaveChangesAsync();

            record.Student = student;
            record.AttendanceEvent = attendanceEvent;

            return Ok(AttendanceRecordDto.FromRecord(record));
        }
    }

    public record ScanAttendanceRequest(
        string? StudentNo,
        string? RfidUid,
        string EventTitle,
        string Status,
        string? Location,
        string? Remarks);

    public record AttendanceRecordDto(
        int Id,
        string Event,
        int StudentId,
        string StudentNo,
        string StudentName,
        string Status,
        DateTime RecordedAt)
    {
        public static AttendanceRecordDto FromRecord(AttendanceRecord record)
        {
            var studentName = record.Student is null
                ? "Unknown student"
                : $"{record.Student.FirstName} {record.Student.LastName}".Trim();

            return new AttendanceRecordDto(
                record.Id,
                record.AttendanceEvent?.Title ?? "Attendance",
                record.StudentId,
                record.Student?.StudentId ?? string.Empty,
                studentName,
                record.Status.ToString(),
                record.RecordedAt);
        }
    }
}
