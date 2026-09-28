using System.ComponentModel.DataAnnotations;

namespace DepartmentFinancialRecords.API.Models
{
    public class DepartmentBill
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(160)]
        public string Category { get; set; } = string.Empty;

        [Required]
        public decimal Amount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}