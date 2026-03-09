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
    public decimal MonthlySalary { get; set; }
    public decimal Bonus { get; set; }
    public decimal TotalSalary { get; set; }
    public string SalaryNotes { get; set; }
    public DateTime Date { get; set; }
    public long Amount { get; set; }

}
}