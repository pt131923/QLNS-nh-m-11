using System.ComponentModel.DataAnnotations;

namespace API.DTOs
{
    public class EmployeeDto
    {
        public int EmployeeId { get; set; }

        [Required(ErrorMessage = "Employee name is required")]
        public string EmployeeName { get; set; }

        public int DepartmentId { get; set; }
    }
}
