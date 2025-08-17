using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace API.Entities
{
    public class Contract
    {
        [Key]
        public int ContractId { get; set; }

        [MaxLength(100)]
        public string ContractName { get; set; }

        public string EmployeeName { get; set; }

        [JsonIgnore]
        public Employee Employee { get; set; }

        [MaxLength(50)]
        public string ContractType { get; set; }

        public int BasicSalary { get; set; }
        public int Allowance { get; set; }

        public DateTime CreateAt { get; set; }
        public DateTime UpdateAt { get; set; }
        public string JobDescription { get; set; }
        public string ContractTerm { get; set; }
        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }
        public string WorkLocation { get; set; }
        public string Leaveofabsence { get; set; }
    }
}
