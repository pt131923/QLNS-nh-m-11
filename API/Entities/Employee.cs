using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace API.Entities
{
    public class Employee
    {
        [Key]
        public int EmployeeId { get; set; }

        [Required]
        public string EmployeeName { get; set; }

        [EmailAddress]
        public string EmployeeEmail { get; set; }

        public string EmployeePhone { get; set; }

        public string EmployeeAddress { get; set; }

        public string EmployeeInformation { get; set; }

        // Thông tin chi tiết
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

        // Liên kết phòng ban
        [ForeignKey("Department")]
        public int DepartmentId { get; set; }

        public AppDepartment Department { get; set; }

        // Hợp đồng
        [JsonIgnore]
        public ICollection<Contract> Contracts { get; set; } = new List<Contract>();

        // Lương
        [JsonIgnore]
        public ICollection<Salary> Salaries { get; set; } = new List<Salary>();

        [JsonIgnore]
        public ICollection<TimeKeeping> TimeKeeping { get; set; } = new List<TimeKeeping>();
    }
}
