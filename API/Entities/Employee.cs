using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace API.Entities
{
    public class Employee
    {
        [Key]
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }

        [ForeignKey("Department")]
        public int DepartmentId { get; set; }
        public virtual AppDepartment Department { get; set; }

        [JsonIgnore]
        public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();

        [JsonIgnore]
        public virtual ICollection<Salary> Salaries { get; set; } = new List<Salary>();
    }
}