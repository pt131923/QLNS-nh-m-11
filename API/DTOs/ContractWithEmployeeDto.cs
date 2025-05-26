namespace API.DTOs
{
    public class ContractWithEmployeeDto
    {
        public int ContractId { get; set; }
        public string ContractName { get; set; }
        public int EmployeeId { get; set; }
        public string ContractType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}

