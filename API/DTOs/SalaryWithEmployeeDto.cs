public class SalaryWithEmployeeDto
{
    public int EmployeeId { get; set; }
    public int SalaryId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }
    public decimal MonthlySalary { get; set; }
    public decimal BasicSalary { get; set; }
    public decimal Allowance { get; set; }
    public decimal GrossSalary { get; set; }
    public decimal TotalInsuranceEmployee { get; set; }
    public decimal PersonalIncomeTax { get; set; }
    public decimal NetSalary { get; set; }
    public decimal Bonus { get; set; }
    public decimal TotalSalary { get; set; }
    public DateTime Date { get; set; }
    public string SalaryNotes { get; set; } = string.Empty;
}
