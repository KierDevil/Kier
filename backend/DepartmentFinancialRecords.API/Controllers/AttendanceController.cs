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

        [HttpGet("events")]
        public async Task<ActionResult<IEnumerable<AttendanceEventDto>>> GetEvents()
        {
            var events = await _dbContext.AttendanceEvents
                .OrderByDescending(item => item.EventDate)
                .Select(item => new AttendanceEventDto(
                    item.Id,
                    item.Title,
                    item.EventDate,
                    item.Location,
                    item.Description,
                    _dbContext.AttendanceRecords.Count(record => record.AttendanceEventId == item.Id)))
                .ToListAsync();

            return Ok(events);
        }

        [HttpPost("events")]
        public async Task<ActionResult<AttendanceEventDetailDto>> CreateEvent(CreateAttendanceEventRequest request)
        {
            var title = request.Title.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                return BadRequest(new { message = "Event title is required." });
            }

            var existingEvent = await _dbContext.AttendanceEvents
                .FirstOrDefaultAsync(item => item.Title == title && item.EventDate.Date == (request.EventDate ?? DateTime.UtcNow).Date);

            if (existingEvent is not null)
            {
                return Conflict(new { message = "An event with the same title and date already exists." });
            }

            var newEvent = new AttendanceEvent
            {
                Title = title,
                EventDate = request.EventDate ?? DateTime.UtcNow,
                Location = request.Location?.Trim() ?? string.Empty,
                Description = request.Description?.Trim() ?? string.Empty
            };

            _dbContext.AttendanceEvents.Add(newEvent);
            await _dbContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEvent), new { id = newEvent.Id }, new AttendanceEventDetailDto(
                newEvent.Id,
                newEvent.Title,
                newEvent.EventDate,
                newEvent.Location,
                newEvent.Description,
                0,
                Array.Empty<AttendanceRecordDto>()));
        }

        [HttpGet("events/{id:int}")]
        public async Task<ActionResult<AttendanceEventDetailDto>> GetEvent(int id)
        {
            var eventItem = await _dbContext.AttendanceEvents
                .FirstOrDefaultAsync(item => item.Id == id);

            if (eventItem is null)
            {
                return NotFound(new { message = "Attendance event not found." });
            }

            var records = await _dbContext.AttendanceRecords
                .Include(record => record.Student)
                .Include(record => record.AttendanceEvent)
                .Where(record => record.AttendanceEventId == id)
                .OrderByDescending(record => record.RecordedAt)
                .ToListAsync();

            return Ok(new AttendanceEventDetailDto(
                eventItem.Id,
                eventItem.Title,
                eventItem.EventDate,
                eventItem.Location,
                eventItem.Description,
                records.Count,
                records.Select(record => AttendanceRecordDto.FromRecord(record)).ToArray()));
        }

        [HttpGet("events/{id:int}/records")]
        public async Task<ActionResult<IEnumerable<AttendanceRecordDto>>> GetEventRecords(int id)
        {
            var eventItem = await _dbContext.AttendanceEvents.FirstOrDefaultAsync(item => item.Id == id);
            if (eventItem is null)
            {
                return NotFound(new { message = "Attendance event not found." });
            }

            var records = await _dbContext.AttendanceRecords
                .Include(record => record.Student)
                .Include(record => record.AttendanceEvent)
                .Where(record => record.AttendanceEventId == id)
                .OrderByDescending(record => record.RecordedAt)
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
                return Ok(AttendanceRecordDto.FromRecord(existingRecord, isDuplicate: true));
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

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                _dbContext.ChangeTracker.Clear();
                var concurrentRecord = await _dbContext.AttendanceRecords
                    .Include(item => item.Student)
                    .Include(item => item.AttendanceEvent)
                    .FirstOrDefaultAsync(item =>
                        item.StudentId == student.Id &&
                        item.AttendanceEventId == attendanceEvent.Id);

                if (concurrentRecord is null)
                {
                    throw;
                }

                return Ok(AttendanceRecordDto.FromRecord(concurrentRecord, isDuplicate: true));
            }

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

    public record CreateAttendanceEventRequest(
        string Title,
        DateTime? EventDate,
        string? Location,
        string? Description);

    public record AttendanceEventDto(
        int Id,
        string Title,
        DateTime EventDate,
        string Location,
        string Description,
        int TotalRecords);

    public record AttendanceEventDetailDto(
        int Id,
        string Title,
        DateTime EventDate,
        string Location,
        string Description,
        int TotalRecords,
        AttendanceRecordDto[] Records);

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
        decimal LateFineAmount,
        bool IsDuplicate)
    {
        public static AttendanceRecordDto FromRecord(
            AttendanceRecord record,
            int minutesLate = 0,
            decimal lateFineAmount = 0,
            bool isDuplicate = false)
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
                lateFineAmount,
                isDuplicate);
        }
    }
}
