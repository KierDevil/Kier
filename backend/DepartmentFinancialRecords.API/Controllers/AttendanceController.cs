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
                .ToListAsync();

            return Ok(records.Select(record => AttendanceRecordDto.FromRecord(record)));
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

            var now = DateTime.Now;
            if (request.OpenAt.HasValue && now < request.OpenAt.Value)
            {
                return BadRequest(new { message = $"Attendance opens at {request.OpenAt.Value:g}." });
            }

            if (request.CloseAt.HasValue && now > request.CloseAt.Value)
            {
                return BadRequest(new { message = $"Attendance closed at {request.CloseAt.Value:g}." });
            }

            var title = string.IsNullOrWhiteSpace(request.EventTitle) ? "Attendance Scan" : request.EventTitle.Trim();
            var attendanceEvent = await _dbContext.AttendanceEvents
                .FirstOrDefaultAsync(item => item.Title == title && item.EventDate.Date == DateTime.UtcNow.Date);

            if (attendanceEvent is null)
            {
                attendanceEvent = new AttendanceEvent
                {
                    Title = title,
                    EventDate = request.OpenAt?.ToUniversalTime() ?? DateTime.UtcNow,
                    Location = request.Location?.Trim() ?? string.Empty,
                    Description = "Created from QR/RFID attendance scan."
                };
                _dbContext.AttendanceEvents.Add(attendanceEvent);
                await _dbContext.SaveChangesAsync();
            }

            var status = Enum.TryParse<AttendanceStatus>(request.Status, true, out var parsedStatus)
                ? parsedStatus
                : AttendanceStatus.Present;
            var minutesLate = 0;
            if (status == AttendanceStatus.Present && request.LateAt.HasValue && now >= request.LateAt.Value)
            {
                status = AttendanceStatus.Late;
                minutesLate = Math.Max(1, (int)Math.Ceiling((now - request.LateAt.Value).TotalMinutes));
            }
            else if (status == AttendanceStatus.Late && request.LateAt.HasValue)
            {
                minutesLate = Math.Max(1, (int)Math.Ceiling((now - request.LateAt.Value).TotalMinutes));
            }

            var lateFineAmount = CalculateLateFine(status, minutesLate, request.FinePerLateMinute, request.MaxLateFine);

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
                existingRecord.Remarks = BuildRemarks(request.Remarks, minutesLate, lateFineAmount, "Updated by QR/RFID scan.");
                await UpsertLateFine(student.Id, title, lateFineAmount, minutesLate);
                await _dbContext.SaveChangesAsync();

                return Ok(AttendanceRecordDto.FromRecord(existingRecord, minutesLate, lateFineAmount));
            }

            var record = new AttendanceRecord
            {
                StudentId = student.Id,
                AttendanceEventId = attendanceEvent.Id,
                Status = status,
                RecordedAt = DateTime.UtcNow,
                Remarks = BuildRemarks(request.Remarks, minutesLate, lateFineAmount, "Recorded by QR/RFID scan.")
            };

            _dbContext.AttendanceRecords.Add(record);
            await UpsertLateFine(student.Id, title, lateFineAmount, minutesLate);
            await _dbContext.SaveChangesAsync();

            record.Student = student;
            record.AttendanceEvent = attendanceEvent;

            return Ok(AttendanceRecordDto.FromRecord(record, minutesLate, lateFineAmount));
        }

        private async Task UpsertLateFine(int studentId, string eventTitle, decimal amount, int minutesLate)
        {
            var category = $"Late Attendance - {eventTitle}";
            var existingFine = await _dbContext.Fines.FirstOrDefaultAsync(fine =>
                fine.StudentId == studentId &&
                fine.Category == category &&
                !fine.IsPaid);

            if (amount <= 0)
            {
                return;
            }

            if (existingFine is null)
            {
                _dbContext.Fines.Add(new Fine
                {
                    StudentId = studentId,
                    Category = category,
                    Amount = amount,
                    Remarks = $"{minutesLate} minute(s) late.",
                    DateIssued = DateTime.UtcNow,
                    IsPaid = false
                });
                return;
            }

            existingFine.Amount = amount;
            existingFine.Remarks = $"{minutesLate} minute(s) late.";
            existingFine.DateIssued = DateTime.UtcNow;
        }

        private static decimal CalculateLateFine(AttendanceStatus status, int minutesLate, decimal? finePerLateMinute, decimal? maxLateFine)
        {
            if (status != AttendanceStatus.Late || minutesLate <= 0 || !finePerLateMinute.HasValue)
            {
                return 0;
            }

            var amount = minutesLate * finePerLateMinute.Value;
            return maxLateFine.HasValue && maxLateFine.Value > 0
                ? Math.Min(amount, maxLateFine.Value)
                : amount;
        }

        private static string BuildRemarks(string? remarks, int minutesLate, decimal lateFineAmount, string fallback)
        {
            var baseRemarks = string.IsNullOrWhiteSpace(remarks) ? fallback : remarks.Trim();
            if (lateFineAmount <= 0)
            {
                return baseRemarks;
            }

            return $"{baseRemarks} Late by {minutesLate} minute(s). Fine: {lateFineAmount:0.##}.";
        }
    }

    public record ScanAttendanceRequest(
        string? StudentNo,
        string? RfidUid,
        string EventTitle,
        string Status,
        DateTime? OpenAt,
        DateTime? LateAt,
        DateTime? CloseAt,
        decimal? FinePerLateMinute,
        decimal? MaxLateFine,
        string? Location,
        string? Remarks);

    public record AttendanceRecordDto(
        int Id,
        string Event,
        int StudentId,
        string StudentNo,
        string StudentName,
        string Status,
        DateTime RecordedAt,
        int MinutesLate,
        decimal LateFineAmount)
    {
        public static AttendanceRecordDto FromRecord(AttendanceRecord record, int minutesLate = 0, decimal lateFineAmount = 0)
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
                record.RecordedAt,
                minutesLate,
                lateFineAmount);
        }
    }
}
