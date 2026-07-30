using System.ComponentModel.DataAnnotations;

namespace DepartmentFinancialRecords.API.Models
{
    public class Fine
    {
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }
        public Student? Student { get; set; }

        [Required]
        public string Category { get; set; } = string.Empty;

        [Required]
        public decimal Amount { get; set; }

        public DateTime DateIssued { get; set; } = DateTime.UtcNow;
        public string Remarks { get; set; } = string.Empty;
        public bool IsPaid { get; set; }
    }
}
