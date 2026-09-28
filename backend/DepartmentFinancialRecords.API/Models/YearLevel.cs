using System.ComponentModel.DataAnnotations;

namespace DepartmentFinancialRecords.API.Models
{
    public class YearLevel
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(80)]
        public string Name { get; set; } = string.Empty;
    }
}