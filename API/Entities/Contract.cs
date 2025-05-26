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
        public string ContractName { get; set; }

        [ForeignKey("Employee")]
        public int EmployeeId { get; set; }
        [JsonIgnore]
        public Employee Employee { get; set; }
        public string ContractType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
