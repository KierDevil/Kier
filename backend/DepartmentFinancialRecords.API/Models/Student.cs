using System.ComponentModel.DataAnnotations;

namespace DepartmentFinancialRecords.API.Models
{
    public class Student
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(64)]
        public string StudentId { get; set; } = string.Empty;

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        public string Course { get; set; } = string.Empty;
        public string YearLevel { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        [MaxLength(128)]
        public string RfidUid { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}
