using System.ComponentModel.DataAnnotations;

namespace DepartmentFinancialRecords.API.Models
{
    public class AttendanceEvent
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public DateTime EventDate { get; set; } = DateTime.UtcNow;
        public string Location { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
