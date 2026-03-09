using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace API.Entities
{
    public class AppDepartment
    {
    [Key]
    [BsonId]
    [BsonRepresentation(BsonType.Int32)]
    public int DepartmentId { get; set; }
    public string Name { get; set; }
    public int SlNhanVien { get; set; }
    public string Description { get; set; }
    public string Addresses { get; set; }
    public string Notes { get; set; }
    public List<Employee> Employee { get; set; } = [];

    }
}

