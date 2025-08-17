namespace API.DTOs
{
    public class ContractDto
    {
        public int ContractId { get; set; }
        public string ContractName { get; set; }

        public string EmployeeName { get; set; }

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
