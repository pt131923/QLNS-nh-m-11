public class SalaryDto
{
    public int SalaryId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; }
    public decimal MonthlySalary { get; set; }
    public decimal Bonus { get; set; }
    public decimal TotalSalary { get; set; }
    public string SalaryNotes { get; set; }
    public DateTime Date { get; set; }
}