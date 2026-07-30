using System.ComponentModel.DataAnnotations;

namespace DepartmentFinancialRecords.API.Models
{
    public class Disbursement
    {
        public int Id { get; set; }

        [Required]
        public string Payee { get; set; } = string.Empty;

        [Required]
        public decimal Amount { get; set; }

        public DateTime DateReleased { get; set; } = DateTime.UtcNow;
        public string Purpose { get; set; } = string.Empty;
        public string DocumentPath { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
    }
}
