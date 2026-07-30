using System.ComponentModel.DataAnnotations;

namespace DepartmentFinancialRecords.API.Models
{
    public enum AttendanceStatus
    {
        Present,
        Absent,
        Late,
        Excused
    }

    public class AttendanceRecord
    {
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }
        public Student? Student { get; set; }

        [Required]
        public int AttendanceEventId { get; set; }
        public AttendanceEvent? AttendanceEvent { get; set; }

        [Required]
        public AttendanceStatus Status { get; set; }

        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
        public string Remarks { get; set; } = string.Empty;
    }
}
