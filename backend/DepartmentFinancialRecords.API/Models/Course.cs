using System.ComponentModel.DataAnnotations;

namespace DepartmentFinancialRecords.API.Models
{
    public class Course
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(120)]
        public string Name { get; set; } = string.Empty;
    }
}