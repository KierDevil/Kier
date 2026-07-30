using System.ComponentModel.DataAnnotations;

namespace DepartmentFinancialRecords.API.Models
{
    public class Collection
    {
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }
        public Student? Student { get; set; }

        [Required]
        public decimal AmountPaid { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
        public string CollectorName { get; set; } = string.Empty;
        public string ReceiptNumber { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }
}
