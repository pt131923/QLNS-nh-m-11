using System.ComponentModel.DataAnnotations;

namespace API.DTOs
{
    public class EmployeeDto
    {
        public int EmployeeId { get; set; }

        [Required(ErrorMessage = "Employee name is required")]
        public string EmployeeName { get; set; }

        public string EmployeeEmail { get; set; }

        public string EmployeePhone { get; set; }

        public string EmployeeAddress { get; set; }

        public int DepartmentId { get; set; }
        public string Department { get; set; }

        public DateTime? BirthDate { get; set; }

        public string PlaceOfBirth { get; set; }

        public string Gender { get; set; }

        public string MaritalStatus { get; set; }

        public string IdentityNumber { get; set; }

        public DateTime? IdentityIssuedDate { get; set; }

        public string IdentityIssuedPlace { get; set; }

        public string Religion { get; set; }

        public string Ethnicity { get; set; }

        public string Nationality { get; set; }

        public string EducationLevel { get; set; }

        public string Specialization { get; set; }
    }
}
