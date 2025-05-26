public class SalaryWithEmployeeDto
{
    public int EmployeeId { get; set; }
    public int SalaryId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public decimal MonthlySalary { get; set; }
    public decimal Bonus { get; set; }
    public decimal TotalSalary { get; set; }
    public DateTime Date { get; set; }
    public string SalaryNotes { get; set; } = string.Empty;
}