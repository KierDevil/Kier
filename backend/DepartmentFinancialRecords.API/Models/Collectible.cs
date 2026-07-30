using System.ComponentModel.DataAnnotations;

namespace DepartmentFinancialRecords.API.Models
{
    public class Collectible
    {
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }
        public Student? Student { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public decimal AmountDue { get; set; }

        public DateTime DueDate { get; set; } = DateTime.UtcNow;
        public bool IsPaid { get; set; }
    }
}
