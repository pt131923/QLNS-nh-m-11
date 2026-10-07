using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace API.Entities
{
public class Salary
{
    [BsonId]
    [BsonRepresentation(BsonType.Int32)]
    public int SalaryId { get; set; }

    [ForeignKey("Employee")]
    public int EmployeeId { get; set; }

    [JsonIgnore]
    public Employee Employee { get; set; }

    public string EmployeeName { get; set; }
    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }
    public decimal MonthlySalary { get; set; }
    public decimal BasicSalary { get; set; }
    public decimal Allowance { get; set; }
    public decimal InsuranceSalary { get; set; }
    public decimal StandardDays { get; set; }
    public decimal WorkedDays { get; set; }
    public decimal UnpaidLeaveDays { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal OvertimePay { get; set; }
    public decimal Bonus { get; set; }
    public decimal OtherDeductions { get; set; }
    public int DependentCount { get; set; }
    public bool DeductUnionFee { get; set; }
    public decimal GrossSalary { get; set; }
    public decimal BhxhEmployee { get; set; }
    public decimal BhytEmployee { get; set; }
    public decimal BhtnEmployee { get; set; }
    public decimal UnionFeeEmployee { get; set; }
    public decimal TotalInsuranceEmployee { get; set; }
    public decimal PersonalDeductionAmount { get; set; }
    public decimal DependentDeduction { get; set; }
    public decimal TaxableIncome { get; set; }
    public decimal PersonalIncomeTax { get; set; }
    public decimal EmployerBhxh { get; set; }
    public decimal EmployerBhyt { get; set; }
    public decimal EmployerBhtn { get; set; }
    public decimal EmployerUnion { get; set; }
    public decimal TotalEmployerInsurance { get; set; }
    public decimal CompanyCost { get; set; }
    public decimal NetSalary { get; set; }
    public decimal TotalSalary { get; set; }
    public string SalaryNotes { get; set; }
    public DateTime Date { get; set; }
    public long Amount { get; set; }
    public string FormulaNotes { get; set; }
  }
}
