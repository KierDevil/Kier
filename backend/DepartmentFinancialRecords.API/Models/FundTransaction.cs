using System.ComponentModel.DataAnnotations;

namespace DepartmentFinancialRecords.API.Models
{
    public enum FundTransactionType
    {
        BeginningBalance,
        Addition,
        Deduction
    }

    public class FundTransaction
    {
        public int Id { get; set; }

        [Required]
        public FundTransactionType TransactionType { get; set; }

        [Required]
        public decimal Amount { get; set; }

        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
        public string Source { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
    }
}
