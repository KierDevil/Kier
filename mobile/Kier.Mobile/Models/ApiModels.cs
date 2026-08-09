namespace Kier.Mobile.Models;

public record LoginRequest(string Username, string Password);
public record LoginResponse(int UserId, string Username, string Role, string Token);
public record ScanAttendanceRequest(string? StudentNo, string? RfidUid, string EventTitle, string Status, DateTime? OpenAt, DateTime? LateAt, DateTime? CloseAt, decimal? FinePerLateMinute, decimal? MaxLateFine, string? Location, string? Remarks);
public record AttendanceEventDto(int Id, string Title, DateTime EventDate, string Location, string Description, int RecordCount);
public record AttendanceRecordDto(int StudentId, string StudentNo, string StudentName, string Status, DateTime RecordedAt, string Remarks);
public record CreateStudentRequest(string StudentNo, string FirstName, string LastName, string Course, string YearLevel, string ContactNumber, string? Email, string? RfidUid);
public record StudentDto(int Id, string StudentNo, string FirstName, string LastName, string Name, string Course, string YearLevel, string ContactNumber, string Email, string RfidUid);
public record CreateCollectionRequest(int StudentId, decimal AmountPaid, DateTime? PaymentDate, string? CollectorName, string? ReceiptNumber, string? Category);
public record CollectionDto(int Id, int StudentId, string StudentName, decimal AmountPaid, DateTime PaymentDate, string CollectorName, string ReceiptNumber, string Category);
